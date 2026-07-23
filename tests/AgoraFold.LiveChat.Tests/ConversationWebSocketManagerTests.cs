using System.Net.WebSockets;
using AgoraFold.LiveChat.Protocol;
using AgoraFold.LiveChat.Transport;

namespace AgoraFold.LiveChat.Tests;

public sealed class ConversationWebSocketManagerTests
{
    private static ConversationWebSocketEvent MessageEvent(int id = 1) =>
        ConversationWebSocketEvent.CreateMessage(
            new ConversationWebSocketMessage(id, "sender-1", "Sender", "hello", DateTime.UtcNow));

    [Fact]
    public async Task BroadcastDoesNotWaitForAStalledPeer()
    {
        var manager = new ConversationWebSocketManager();
        var stalledSocket = new StubWebSocket { BlockSends = true };
        var healthySocket = new StubWebSocket();
        var stalled = manager.Add(1, stalledSocket, CancellationToken.None);
        var healthy = manager.Add(1, healthySocket, CancellationToken.None);

        // The broadcast must complete promptly even though the stalled peer's socket write
        // never finishes — this is what keeps the REST reply endpoint responsive.
        var broadcast = manager.BroadcastAsync(1, MessageEvent());
        Assert.Same(broadcast, await Task.WhenAny(broadcast, Task.Delay(TimeSpan.FromSeconds(2))));

        // The healthy peer still receives the event via its own pump.
        await healthySocket.WaitForFirstSendAsync(TimeSpan.FromSeconds(5));

        stalledSocket.Abort();
        await stalled.DisposeAsync();
        await healthy.DisposeAsync();
    }

    [Fact]
    public async Task SendTimesOutAndAbortsTheConnectionWhenThePeerStopsReading()
    {
        var socket = new StubWebSocket { BlockSends = true };
        await using var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromMilliseconds(200));

        var sent = await connection.SendAsync(MessageEvent());

        Assert.False(sent);
        Assert.True(socket.Aborted);
    }

    [Fact]
    public async Task OverflowingTheOutboundQueueFailsTheConnection()
    {
        var socket = new StubWebSocket { BlockSends = true };
        // Long send timeout so the pump stays stuck in its first send while the queue fills.
        var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromSeconds(30));

        var accepted = 0;
        while (accepted < 500 && connection.TryEnqueue(MessageEvent(accepted)))
        {
            accepted++;
        }

        // The bounded queue rejects the overflow and the connection is failed (aborted) so
        // its client reconnects instead of staying subscribed with missed events.
        Assert.True(accepted < 500);
        Assert.True(socket.Aborted);
        Assert.False(connection.TryEnqueue(MessageEvent()));

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task RemovingTheLastConnectionCannotOrphanAConcurrentlyAddedOne()
    {
        // Regression: Remove used to check the conversation bucket for emptiness and then
        // remove it from the top-level map — an Add landing between the two steps went into
        // a bucket that broadcasts could no longer find, permanently orphaning a connection
        // whose client had already been promised delivery via 'connected'.
        var manager = new ConversationWebSocketManager();

        for (var i = 0; i < 5000; i++)
        {
            var first = manager.Add(1, new StubWebSocket(), CancellationToken.None);
            var newSocket = new StubWebSocket();
            ConversationWebSocketConnection second = null!;

            using var barrier = new Barrier(2);
            await Task.WhenAll(
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    manager.Remove(1, first);
                }),
                Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    second = manager.Add(1, newSocket, CancellationToken.None);
                }));

            await manager.BroadcastAsync(1, MessageEvent());

            // An orphaned connection never receives the broadcast.
            await newSocket.WaitForFirstSendAsync(TimeSpan.FromSeconds(5));

            manager.Remove(1, second);
            await first.DisposeAsync();
            await second.DisposeAsync();
        }
    }

    [Fact]
    public async Task DisposeSurvivesAnUnexpectedSendFault()
    {
        var socket = new StubWebSocket { ThrowOnSends = true };
        var connection = new ConversationWebSocketConnection(socket, CancellationToken.None);

        Assert.True(connection.TryEnqueue(MessageEvent()));

        // The pump must contain the fault and fail the connection instead of letting it
        // propagate — endpoint cleanup awaits this dispose, so a faulted pump task would
        // throw in the middle of the request's teardown.
        await connection.DisposeAsync();
        Assert.True(socket.Aborted);
    }

    [Fact]
    public async Task OversizedPayloadClosureCannotOverlapAnInFlightPumpSend()
    {
        // Regression: oversized-payload closure used to call CloseSocketAsync(connection.Socket)
        // directly, writing the close frame straight to the socket while the pump could still be
        // mid-SendAsync for a queued broadcast. The fix routes it through connection.CloseAsync,
        // which shares the connection's send lock with the pump.
        var socket = new StubWebSocket { BlockSends = true };
        var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromSeconds(5));

        Assert.True(connection.TryEnqueue(MessageEvent()));
        await socket.WaitForFirstSendAsync(TimeSpan.FromSeconds(5)); // the pump's send is now in flight

        var closeTask = connection.CloseAsync(WebSocketCloseStatus.MessageTooBig, "The message payload is too large.");

        // Give the close a real chance to contend for the lock before the stalled peer "resumes".
        await Task.Delay(100);
        socket.CompletePendingSends();

        await closeTask;

        Assert.False(socket.ObservedOverlap);
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task EndpointCleanupCannotOverlapAnInFlightPumpSend()
    {
        // Regression: endpoint cleanup used to close the raw socket before disposing the
        // connection, so the final close frame could interleave with a pump send still
        // draining the outbound queue. The fix disposes first (which drains the pump) and
        // only then calls the endpoint's own CloseSocketAsync.
        var socket = new StubWebSocket { BlockSends = true };
        var connection = new ConversationWebSocketConnection(
            socket, CancellationToken.None, sendTimeout: TimeSpan.FromSeconds(5));

        Assert.True(connection.TryEnqueue(MessageEvent()));
        await socket.WaitForFirstSendAsync(TimeSpan.FromSeconds(5)); // the pump's send is now in flight

        var disposeTask = connection.DisposeAsync().AsTask();

        // Give dispose a real chance to be waiting on the stalled pump send before it resolves.
        await Task.Delay(100);
        socket.CompletePendingSends();

        await disposeTask; // mirrors the endpoint's finally block: dispose, then close.
        await ConversationWebSocketEndpoint.CloseSocketAsync(socket);

        Assert.False(socket.ObservedOverlap);
    }
}

/// <summary>
/// A WebSocket whose sends either complete immediately or, with <see cref="BlockSends"/>,
/// hang until cancelled or aborted — simulating a peer that stops reading so the TCP
/// window fills and socket writes never finish.
/// </summary>
internal sealed class StubWebSocket : WebSocket
{
    private readonly object gate = new();
    private readonly List<TaskCompletionSource> pendingSends = [];
    private readonly TaskCompletionSource firstSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private WebSocketState state = WebSocketState.Open;

    // Counts calls to SendAsync/CloseAsync/CloseOutputAsync that are currently "on the wire" —
    // from entry until their returned task completes — so tests can prove the connection's
    // send lock actually serializes pump sends against closes instead of just hoping timing
    // never overlaps them.
    private int activeWireOperations;

    public bool BlockSends { get; init; }
    public bool ThrowOnSends { get; init; }
    public bool Aborted { get; private set; }
    public bool ObservedOverlap { get; private set; }

    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => state;
    public override string? SubProtocol => null;

    public Task WaitForFirstSendAsync(TimeSpan timeout) => firstSend.Task.WaitAsync(timeout);

    /// <summary>Resolves every send currently blocked on <see cref="BlockSends"/>, as if the peer resumed reading.</summary>
    public void CompletePendingSends()
    {
        lock (gate)
        {
            foreach (var pending in pendingSends)
            {
                pending.TrySetResult();
            }

            pendingSends.Clear();
        }
    }

    public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        firstSend.TrySetResult();
        EnterWireOperation();
        try
        {
            if (ThrowOnSends)
            {
                // An exception type outside the WebSocket contract, simulating an unexpected
                // fault that the outbound pump must contain rather than propagate.
                throw new InvalidOperationException("Unexpected send fault.");
            }

            if (!BlockSends)
            {
                return;
            }

            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (Aborted)
                {
                    throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
                }

                pendingSends.Add(pending);
            }

            using var registration = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            await pending.Task;
        }
        finally
        {
            ExitWireOperation();
        }
    }

    public override void Abort()
    {
        lock (gate)
        {
            Aborted = true;
            state = WebSocketState.Aborted;
            foreach (var pending in pendingSends)
            {
                pending.TrySetException(new WebSocketException(WebSocketError.ConnectionClosedPrematurely));
            }

            pendingSends.Clear();
        }
    }

    public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        EnterWireOperation();
        try
        {
            state = WebSocketState.Closed;
        }
        finally
        {
            ExitWireOperation();
        }

        await Task.CompletedTask;
    }

    public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        EnterWireOperation();
        try
        {
            state = WebSocketState.CloseSent;
        }
        finally
        {
            ExitWireOperation();
        }

        await Task.CompletedTask;
    }

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
        new TaskCompletionSource<WebSocketReceiveResult>().Task;

    public override void Dispose()
    {
    }

    private void EnterWireOperation()
    {
        if (Interlocked.Increment(ref activeWireOperations) > 1)
        {
            ObservedOverlap = true;
        }
    }

    private void ExitWireOperation() => Interlocked.Decrement(ref activeWireOperations);
}
