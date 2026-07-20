<script setup>
const props = defineProps({
  page: { type: Number, required: true },
  totalPages: { type: Number, required: true },
  hasPreviousPage: { type: Boolean, required: true },
  hasNextPage: { type: Boolean, required: true },
})

const emit = defineEmits(['change'])
</script>

<template>
  <div class="pagination" v-if="totalPages > 1">
    <button class="secondary" :disabled="!hasPreviousPage" @click="emit('change', props.page - 1)">Previous</button>
    <span>Page {{ page }} of {{ totalPages }}</span>
    <button class="secondary" :disabled="!hasNextPage" @click="emit('change', props.page + 1)">Next</button>
  </div>
</template>
