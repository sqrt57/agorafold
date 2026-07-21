using AgoraFold.BlazorWasm.Client.Api.Dto.Conversations;

namespace AgoraFold.BlazorWasm.Client.Api;

public sealed class ConversationApiClient(ApiClient api)
{
    public Task<IReadOnlyList<ConversationSummaryResponse>> GetInboxAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<ConversationSummaryResponse>>("api/conversations", cancellationToken);

    public Task<ConversationThreadResponse> GetThreadAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ConversationThreadResponse>($"api/conversations/{id}", cancellationToken);

    public Task<ConversationThreadResponse> StartAsync(int listingId, CancellationToken cancellationToken = default) =>
        api.PostJsonAsync<StartConversationRequest, ConversationThreadResponse>(
            "api/conversations", new StartConversationRequest(listingId), cancellationToken);

    public Task<ConversationThreadResponse> ReplyAsync(int id, string body, CancellationToken cancellationToken = default) =>
        api.PostJsonAsync<ReplyRequest, ConversationThreadResponse>(
            $"api/conversations/{id}/replies", new ReplyRequest(body), cancellationToken);
}
