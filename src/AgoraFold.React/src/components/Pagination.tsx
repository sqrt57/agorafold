interface PaginationProps {
  page: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  onChange: (page: number) => void
}

export default function Pagination({ page, totalPages, hasPreviousPage, hasNextPage, onChange }: PaginationProps) {
  if (totalPages <= 1) return null

  return (
    <div className="pagination">
      <button className="secondary" disabled={!hasPreviousPage} onClick={() => onChange(page - 1)}>
        Previous
      </button>
      <span>Page {page} of {totalPages}</span>
      <button className="secondary" disabled={!hasNextPage} onClick={() => onChange(page + 1)}>
        Next
      </button>
    </div>
  )
}
