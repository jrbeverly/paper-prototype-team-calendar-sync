import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/admin': 'http://127.0.0.1:5000',
      '/teams': 'http://127.0.0.1:5000',
      '/ping': 'http://127.0.0.1:5000',
      '/calendar.ics': 'http://127.0.0.1:5000',
    },
  },
})
