import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import * as conversationsApi from '../api/conversations'
import type { ConversationSummary } from '../api/types'

export default function ConversationsInboxPage() {
  const [conversations, setConversations] = useState<ConversationSummary[]>([])

  useEffect(() => {
    conversationsApi.getInbox().then(setConversations)
  }, [])

  return (
    <>
      <h1>Messages</h1>
      {conversations.length === 0 && <p className="muted">No conversations yet.</p>}
      <ul className="conversation-list">
        {conversations.map((c) => (
          <li key={c.id}>
            <Link to={`/conversations/${c.id}`}>
              <strong>{c.listingTitle}</strong> &middot; {c.otherPartyDisplayName}
            </Link>
            <p className="muted">
              {c.lastMessageIsMine ? 'You: ' : ''}
              {c.lastMessageBody}
            </p>
          </li>
        ))}
      </ul>
    </>
  )
}
