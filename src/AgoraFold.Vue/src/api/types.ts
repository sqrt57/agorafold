export interface Category {
  id: number
  name: string
}

export interface ListingImage {
  id: number
  url: string
}

export interface ListingSummary {
  id: number
  title: string
  price: number | null
  categoryName: string
  thumbnailUrl: string | null
  createdAt: string
}

export interface PagedListings {
  items: ListingSummary[]
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ListingDetail {
  id: number
  title: string
  description: string
  price: number | null
  categoryId: number
  categoryName: string
  ownerId: string
  ownerDisplayName: string
  createdAt: string
  images: ListingImage[]
  isOwner: boolean
  canMessage: boolean
  imageErrors?: string[] | null
}

export interface ConversationSummary {
  id: number
  listingId: number
  listingTitle: string
  otherPartyDisplayName: string
  lastMessageBody: string | null
  lastActivityAt: string
  lastMessageIsMine: boolean
}

export interface ConversationMessage {
  id: number
  senderDisplayName: string
  body: string
  sentAt: string
  isMine: boolean
}

export interface ConversationThread {
  id: number
  listingId: number
  listingTitle: string
  messages: ConversationMessage[]
}

export interface AuthUser {
  id: string
  email: string
  displayName: string
}
