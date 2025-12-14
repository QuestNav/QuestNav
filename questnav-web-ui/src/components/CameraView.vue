<template>
  <div class="camera-view">
    <div class="camera-controls">
      <div class="controls-left">
        <button @click="toggleFullscreen" class="secondary">
          {{ isFullscreen ? '⬜ Exit Fullscreen' : '⛶ Fullscreen' }}
        </button>
      </div>
      <div class="controls-right">
        <span :class="['stream-status', streamEnabled ? 'active' : 'inactive']">
          <span class="status-dot"></span>
          {{ streamEnabled ? 'Stream Active' : 'Stream Disabled' }}
        </span>
      </div>
    </div>

    <div v-if="!streamEnabled" class="stream-disabled-message">
      <div class="disabled-icon">📷</div>
      <h3>Camera Stream Disabled</h3>
      <p>Enable "Passthrough Camera Stream" in Settings to view the camera feed.</p>
    </div>

    <div v-else class="camera-container" ref="cameraContainer">
      <img :src="'./video'" alt="Camera Stream" class="camera-stream" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useConfigStore } from '../stores/config'

const configStore = useConfigStore()
const cameraContainer = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

const streamEnabled = computed(() => {
  return configStore.config?.enablePassthroughStream ?? false
})

function toggleFullscreen() {
  if (!cameraContainer.value) return

  if (!document.fullscreenElement) {
    cameraContainer.value.requestFullscreen().catch(err => {
      console.error('Failed to enter fullscreen:', err)
    })
  } else {
    document.exitFullscreen()
  }
}

function handleFullscreenChange() {
  isFullscreen.value = !!document.fullscreenElement
}

onMounted(() => {
  document.addEventListener('fullscreenchange', handleFullscreenChange)
})

onUnmounted(() => {
  document.removeEventListener('fullscreenchange', handleFullscreenChange)
})
</script>

<style scoped>
.camera-view {
  width: 100%;
  max-width: 1400px;
  margin: 0 auto;
}

.camera-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  padding: 1rem;
  background: var(--card-bg);
  border-radius: 8px;
  border: 1px solid var(--border-color);
}

.controls-left,
.controls-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.stream-status {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  border: 2px solid;
}

.stream-status.active {
  color: var(--success-color);
  background: rgba(76, 175, 80, 0.15);
  border-color: var(--success-color);
}

.stream-status.inactive {
  color: var(--text-secondary);
  background: var(--bg-tertiary);
  border-color: var(--border-color);
}

.stream-status .status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.stream-status.active .status-dot {
  background-color: var(--success-color);
  box-shadow: 0 0 8px var(--success-color);
}

.stream-status.inactive .status-dot {
  background-color: var(--text-secondary);
}

.stream-disabled-message {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  background: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  text-align: center;
}

.disabled-icon {
  font-size: 4rem;
  margin-bottom: 1.5rem;
  filter: grayscale(100%) opacity(0.5);
}

.stream-disabled-message h3 {
  color: var(--text-primary);
  font-size: 1.125rem;
  margin-bottom: 0.5rem;
}

.stream-disabled-message p {
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.camera-container {
  background: #1e1e1e;
  border-radius: 8px;
  border: 1px solid var(--border-color);
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 400px;
}

.camera-container:fullscreen {
  background: #000;
  border-radius: 0;
  border: none;
}

.camera-stream {
  width: 100%;
  height: 100%;
  object-fit: fill;
}

.camera-container:fullscreen .camera-stream {
  max-width: 100vw;
  max-height: 100vh;
}

@media (max-width: 768px) {
  .camera-controls {
    flex-direction: column;
    align-items: stretch;
  }
  
  .controls-left,
  .controls-right {
    justify-content: space-between;
  }
  
  .camera-container {
    min-height: 300px;
  }
}
</style>
