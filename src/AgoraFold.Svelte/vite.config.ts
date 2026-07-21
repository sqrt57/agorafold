import { defineConfig } from 'vite'
import { svelte } from '@sveltejs/vite-plugin-svelte'

export default defineConfig({
  plugins: [svelte()],
  server: {
    port: 5175,
    strictPort: true,
    proxy: {
      '/api': 'http://localhost:5155',
      '/uploads': 'http://localhost:5155',
    },
  },
})
