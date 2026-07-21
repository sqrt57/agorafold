<script lang="ts">
  export let page: number
  export let totalPages: number
  export let hasPreviousPage: boolean
  export let hasNextPage: boolean
  export let onChange: (page: number) => void
</script>

{#if totalPages > 1}
  <div class="pagination">
    <button class="secondary" disabled={!hasPreviousPage} onclick={() => onChange(page - 1)}>Previous</button>
    <span>Page {page} of {totalPages}</span>
    <button class="secondary" disabled={!hasNextPage} onclick={() => onChange(page + 1)}>Next</button>
  </div>
{/if}
