import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    // Cổng riêng để chạy song song với dự án 'hai' (5173)
    port: 5174,
    proxy: {
      // Chuyển tiếp gọi API sang backend khi chạy dev
      '/api': {
        target: process.env.VITE_API_TARGET || 'http://localhost:8081',
        changeOrigin: true
      },
      // SignalR hub — bật ws để proxy WebSocket khi chạy dev
      '/hubs': {
        target: process.env.VITE_API_TARGET || 'http://localhost:8081',
        changeOrigin: true,
        ws: true
      }
    }
  }
})
