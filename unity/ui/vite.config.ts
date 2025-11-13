import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: './',
  
  // Build configuration
  build: {
    outDir: '../Assets/StreamingAssets/ui',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html')
      }
    }
  },
  
  // Development server configuration
  server: {
    port: 5173,
    proxy: {
      // Proxy API requests to Quest device
      '/api': {
        target: process.env.VITE_API_TARGET || 'http://192.168.1.100:18080',
        changeOrigin: true,
        secure: false
      },
      '/ws': {
        target: process.env.VITE_WS_TARGET || 'ws://192.168.1.100:18080',
        ws: true,
        changeOrigin: true
      }
    }
  },
  
  // Path resolution
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  }
})

