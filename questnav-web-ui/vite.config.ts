import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue()],
  base: './',
  
  // Build configuration
  build: {
    outDir: '../unity/Assets/StreamingAssets/ui',
    emptyOutDir: true,
    assetsDir: 'assets',
    rollupOptions: {
      input: {
        main: resolve(import.meta.dirname, 'index.html')
      },
      output: {
        // Use consistent filenames (no hash) for easier Unity integration
        // These filenames are referenced in WebServerManager.cs for Android APK extraction
        // If you change these, update WebServerManager.cs ExtractAndroidUIFiles() method
        entryFileNames: 'assets/[name].js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: 'assets/[name].[ext]'
      }
    }
  },
  
  // Development server configuration
  server: {
    port: 5173,
    proxy: {
      // Proxy API requests to Quest device
      '/api': {
        target: process.env.VITE_API_TARGET || 'http://192.168.1.100:5801',
        changeOrigin: true,
        secure: false
      },
      '/ws': {
        target: process.env.VITE_WS_TARGET || 'ws://192.168.1.100:5801',
        ws: true,
        changeOrigin: true
      }
    }
  },
  
  // Path resolution
  resolve: {
    alias: {
      '@': resolve(import.meta.dirname, 'src')
    }
  }
})