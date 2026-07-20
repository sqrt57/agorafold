<script setup lang="ts">
const props = defineProps<{
  page: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}>()

const emit = defineEmits<{
  change: [page: number]
}>()
</script>

<template>
  <div class="pagination" v-if="totalPages > 1">
    <button class="secondary" :disabled="!hasPreviousPage" @click="emit('change', props.page - 1)">Previous</button>
    <span>Page {{ page }} of {{ totalPages }}</span>
    <button class="secondary" :disabled="!hasNextPage" @click="emit('change', props.page + 1)">Next</button>
  </div>
</template>
