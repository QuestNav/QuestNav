<template>
  <div class="camera-view">
    <div class="camera-controls">
      <div class="controls-row">
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
      <div class="controls-row">
        <div class="controls-left">
          <div class="control-container">
            <label for="stream-mode">Mode:</label>
            <select id="stream-mode" v-model="selectedStreamMode">
              <option v-for="option in streamOptions" :key="option.text" :value="option.value">
                {{ option.text }}
              </option>
            </select>
          </div>
          <div class="control-container">
            <label for="compression">Quality: {{ selectedStreamQuality }}</label>
            <input type="range" id="compression" min="1" max="100" v-model="selectedStreamQuality" />
          </div>
          <button @click="applySettings">Apply</button>
        </div>
        <div class="controls-right">
          <span v-if="streamEnabled" class="active-stream-settings">{{ activeStreamSettings }}</span>
        </div>
      </div>
    </div>

    <div v-if="!streamEnabled" class="stream-disabled-message">
      <div class="disabled-icon">📷</div>
      <h3>Camera Stream Disabled</h3>
      <p>Enable "Passthrough Camera Stream" in Settings to view the camera feed.</p>
    </div>

    <div v-else class="camera-container" ref="cameraContainer">
      <img :src="streamUrl" alt="Camera Stream" class="camera-stream" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useConfigStore } from '../stores/config'
import type { StreamModeModel } from '../types'

const configStore = useConfigStore()
const cameraContainer = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

const streamEnabled = computed(() => {
  return configStore.config?.enablePassthroughStream ?? false
})

const streamOptions = [
  { text: '320x240 MJPEG 1 fps', value: { width: 320, height: 240, framerate: 1 } },
  { text: '320x240 MJPEG 5 fps', value: { width: 320, height: 240, framerate: 5 } },
  { text: '320x240 MJPEG 15 fps', value: { width: 320, height: 240, framerate: 15 } },
  { text: '320x240 MJPEG 24 fps', value: { width: 320, height: 240, framerate: 24 } },
  { text: '320x240 MJPEG 30 fps', value: { width: 320, height: 240, framerate: 30 } },
  { text: '320x240 MJPEG 48 fps', value: { width: 320, height: 240, framerate: 48 } },
  { text: '320x240 MJPEG 60 fps', value: { width: 320, height: 240, framerate: 60 } },
  { text: '640x360 MJPEG 1 fps', value: { width: 640, height: 360, framerate: 1 } },
  { text: '640x360 MJPEG 5 fps', value: { width: 640, height: 360, framerate: 5 } },
  { text: '640x360 MJPEG 15 fps', value: { width: 640, height: 360, framerate: 15 } },
  { text: '640x360 MJPEG 24 fps', value: { width: 640, height: 360, framerate: 24 } },
  { text: '640x360 MJPEG 30 fps', value: { width: 640, height: 360, framerate: 30 } },
  { text: '640x360 MJPEG 48 fps', value: { width: 640, height: 360, framerate: 48 } },
  { text: '640x360 MJPEG 60 fps', value: { width: 640, height: 360, framerate: 60 } },
  { text: '640x480 MJPEG 1 fps', value: { width: 640, height: 480, framerate: 1 } },
  { text: '640x480 MJPEG 5 fps', value: { width: 640, height: 480, framerate: 5 } },
  { text: '640x480 MJPEG 15 fps', value: { width: 640, height: 480, framerate: 15 } },
  { text: '640x480 MJPEG 24 fps', value: { width: 640, height: 480, framerate: 24 } },
  { text: '640x480 MJPEG 30 fps', value: { width: 640, height: 480, framerate: 30 } },
  { text: '640x480 MJPEG 48 fps', value: { width: 640, height: 480, framerate: 48 } },
  { text: '640x480 MJPEG 60 fps', value: { width: 640, height: 480, framerate: 60 } },
  { text: '720x480 MJPEG 1 fps', value: { width: 720, height: 480, framerate: 1 } },
  { text: '720x480 MJPEG 5 fps', value: { width: 720, height: 480, framerate: 5 } },
  { text: '720x480 MJPEG 15 fps', value: { width: 720, height: 480, framerate: 15 } },
  { text: '720x480 MJPEG 24 fps', value: { width: 720, height: 480, framerate: 24 } },
  { text: '720x480 MJPEG 30 fps', value: { width: 720, height: 480, framerate: 30 } },
  { text: '720x480 MJPEG 48 fps', value: { width: 720, height: 480, framerate: 48 } },
  { text: '720x480 MJPEG 60 fps', value: { width: 720, height: 480, framerate: 60 } },
  { text: '720x576 MJPEG 1 fps', value: { width: 720, height: 576, framerate: 1 } },
  { text: '720x576 MJPEG 5 fps', value: { width: 720, height: 576, framerate: 5 } },
  { text: '720x576 MJPEG 15 fps', value: { width: 720, height: 576, framerate: 15 } },
  { text: '720x576 MJPEG 24 fps', value: { width: 720, height: 576, framerate: 24 } },
  { text: '720x576 MJPEG 30 fps', value: { width: 720, height: 576, framerate: 30 } },
  { text: '720x576 MJPEG 48 fps', value: { width: 720, height: 576, framerate: 48 } },
  { text: '720x576 MJPEG 60 fps', value: { width: 720, height: 576, framerate: 60 } },
  { text: '800x600 MJPEG 1 fps', value: { width: 800, height: 600, framerate: 1 } },
  { text: '800x600 MJPEG 5 fps', value: { width: 800, height: 600, framerate: 5 } },
  { text: '800x600 MJPEG 15 fps', value: { width: 800, height: 600, framerate: 15 } },
  { text: '800x600 MJPEG 24 fps', value: { width: 800, height: 600, framerate: 24 } },
  { text: '800x600 MJPEG 30 fps', value: { width: 800, height: 600, framerate: 30 } },
  { text: '800x600 MJPEG 48 fps', value: { width: 800, height: 600, framerate: 48 } },
  { text: '800x600 MJPEG 60 fps', value: { width: 800, height: 600, framerate: 60 } },
  { text: '1024x576 MJPEG 1 fps', value: { width: 1024, height: 576, framerate: 1 } },
  { text: '1024x576 MJPEG 5 fps', value: { width: 1024, height: 576, framerate: 5 } },
  { text: '1024x576 MJPEG 15 fps', value: { width: 1024, height: 576, framerate: 15 } },
  { text: '1024x576 MJPEG 24 fps', value: { width: 1024, height: 576, framerate: 24 } },
  { text: '1024x576 MJPEG 30 fps', value: { width: 1024, height: 576, framerate: 30 } },
  { text: '1024x576 MJPEG 48 fps', value: { width: 1024, height: 576, framerate: 48 } },
  { text: '1024x576 MJPEG 60 fps', value: { width: 1024, height: 576, framerate: 60 } },
  { text: '1280x720 MJPEG 1 fps', value: { width: 1280, height: 720, framerate: 1 } },
  { text: '1280x720 MJPEG 5 fps', value: { width: 1280, height: 720, framerate: 5 } },
  { text: '1280x720 MJPEG 15 fps', value: { width: 1280, height: 720, framerate: 15 } },
  { text: '1280x720 MJPEG 24 fps', value: { width: 1280, height: 720, framerate: 24 } },
  { text: '1280x720 MJPEG 30 fps', value: { width: 1280, height: 720, framerate: 30 } },
  { text: '1280x720 MJPEG 48 fps', value: { width: 1280, height: 720, framerate: 48 } },
  { text: '1280x720 MJPEG 60 fps', value: { width: 1280, height: 720, framerate: 60 } },
  { text: '1280x960 MJPEG 1 fps', value: { width: 1280, height: 960, framerate: 1 } },
  { text: '1280x960 MJPEG 5 fps', value: { width: 1280, height: 960, framerate: 5 } },
  { text: '1280x960 MJPEG 15 fps', value: { width: 1280, height: 960, framerate: 15 } },
  { text: '1280x960 MJPEG 24 fps', value: { width: 1280, height: 960, framerate: 24 } },
  { text: '1280x960 MJPEG 30 fps', value: { width: 1280, height: 960, framerate: 30 } },
  { text: '1280x960 MJPEG 48 fps', value: { width: 1280, height: 960, framerate: 48 } },
  { text: '1280x960 MJPEG 60 fps', value: { width: 1280, height: 960, framerate: 60 } },
  { text: '1280x1080 MJPEG 1 fps', value: { width: 1280, height: 1080, framerate: 1 } },
  { text: '1280x1080 MJPEG 5 fps', value: { width: 1280, height: 1080, framerate: 5 } },
  { text: '1280x1080 MJPEG 15 fps', value: { width: 1280, height: 1080, framerate: 15 } },
  { text: '1280x1080 MJPEG 24 fps', value: { width: 1280, height: 1080, framerate: 24 } },
  { text: '1280x1080 MJPEG 30 fps', value: { width: 1280, height: 1080, framerate: 30 } },
  { text: '1280x1080 MJPEG 48 fps', value: { width: 1280, height: 1080, framerate: 48 } },
  { text: '1280x1080 MJPEG 60 fps', value: { width: 1280, height: 1080, framerate: 60 } },
  { text: '1280x1280 MJPEG 1 fps', value: { width: 1280, height: 1280, framerate: 1 } },
  { text: '1280x1280 MJPEG 5 fps', value: { width: 1280, height: 1280, framerate: 5 } },
  { text: '1280x1280 MJPEG 15 fps', value: { width: 1280, height: 1280, framerate: 15 } },
  { text: '1280x1280 MJPEG 24 fps', value: { width: 1280, height: 1280, framerate: 24 } },
  { text: '1280x1280 MJPEG 30 fps', value: { width: 1280, height: 1280, framerate: 30 } },
  { text: '1280x1280 MJPEG 48 fps', value: { width: 1280, height: 1280, framerate: 48 } },
  { text: '1280x1280 MJPEG 60 fps', value: { width: 1280, height: 1280, framerate: 60 } },
]

const selectedStreamMode = ref<StreamModeModel>({ width: 320, height: 240, framerate: 24 })
const selectedStreamQuality = ref(75)
const cacheBuster = ref(Date.now())

const streamUrl = computed(() => `./video?t=${cacheBuster.value}`)

const activeStreamSettings = computed(() => {
  const mode = configStore.config?.streamMode
  const quality = configStore.config?.streamQuality
  if (!mode) return ''
  return `${mode.width}x${mode.height}@${mode.framerate}fps Quality: ${quality}`
})

function applySettings() {
  configStore.updateStreamMode(selectedStreamMode.value)
  configStore.updateStreamQuality(selectedStreamQuality.value)
  cacheBuster.value = Date.now()
}

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
  if (configStore.config?.streamMode) {
    const matchingOption = streamOptions.find(
      opt =>
        opt.value.width === configStore.config!.streamMode.width &&
        opt.value.height === configStore.config!.streamMode.height &&
        opt.value.framerate === configStore.config!.streamMode.framerate
    )
    if (matchingOption) {
      selectedStreamMode.value = matchingOption.value
    } else {
      selectedStreamMode.value = configStore.config.streamMode
    }
  }
  if (configStore.config?.streamQuality) {
    selectedStreamQuality.value = configStore.config.streamQuality
  }
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
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1rem;
  background: var(--card-bg);
  border-radius: 8px;
  border: 1px solid var(--border-color);
}

.controls-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.controls-left,
.controls-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.active-stream-settings {
  font-size: 0.875rem;
  color: var(--text-secondary);
  background: var(--bg-tertiary);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  border: 1px solid var(--border-color);
}

.control-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
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
