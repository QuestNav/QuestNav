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
    // Don't delete fallback.html or meta files in the output dir
    emptyOutDir: false,
    assetsDir: 'assets',
    rollupOptions: {
      input: {
        main: resolve(__dirname, 'index.html')
      },
      output: {
        // Use consistent filenames (no hash) for easier Unity integration
        // These filenames are referenced in ConfigBootstrap.cs for Android APK extraction
        // If you change these, update ConfigBootstrap.cs ExtractAndroidUIFiles() method
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

