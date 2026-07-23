# Backlog

Known issues and follow-ups that aren't part of any current task. Newest entries at the top.

## React conversation thread: new reply renders out of chronological order

**Where:** `src/AgoraFold.React/src/pages/ConversationThreadPage.tsx`

**Observed:** On an existing thread with messages spanning multiple days, submitting a reply via `sendReply` (which calls `conversationsApi.reply()` and replaces `thread` with the server's response) rendered the just-sent message at the *top* of the list instead of appended at the bottom, even though the server's `ConversationThreadResponse.messages` is ordered by `SentAt` ascending (same `GetThreadAsync` query used by every other frontend).

**Repro:** Log in, open an existing conversation with several prior messages (e.g. conversation 3, "Vintage Bicycle"), send a new reply, and check where it lands in the list relative to the older messages.

**Not yet root-caused** — could be a client-side ordering bug in this page, or something in how the response was captured during ad-hoc testing. Worth a focused look before assuming either way; compare against `AgoraFold.Vue`'s `ConversationThreadView.vue`, which renders `thread.messages` in server order without issue.
