import { Show } from 'solid-js'

interface PaginationProps {
  page: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  onChange: (page: number) => void
}

export default function Pagination(props: PaginationProps) {
  return (
    <Show when={props.totalPages > 1}>
      <div class="pagination">
        <button class="secondary" disabled={!props.hasPreviousPage} onClick={() => props.onChange(props.page - 1)}>
          Previous
        </button>
        <span>Page {props.page} of {props.totalPages}</span>
        <button class="secondary" disabled={!props.hasNextPage} onClick={() => props.onChange(props.page + 1)}>
          Next
        </button>
      </div>
    </Show>
  )
}
