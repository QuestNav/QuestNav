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
              <option v-for="option in streamModeOptions" :key="option.text" :value="option.value">
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
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useConfigStore } from '../stores/config'
import type { VideoModeModel } from '../types'
import { videoApi } from '../api/video'

const configStore = useConfigStore()
const cameraContainer = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

const streamEnabled = computed(() => {
  return configStore.config?.enablePassthroughStream ?? false
})

const streamModeOptions = ref<{ text: string; value: VideoModeModel }[]>([])

const selectedStreamMode = ref<VideoModeModel>({ width: 320, height: 240, framerate: 24 })
const selectedStreamQuality = ref(75)
const cacheBuster = ref(Date.now())

const streamUrl = computed(() => `./video?t=${cacheBuster.value}`)

const activeStreamSettings = computed(() => {
  const mode = configStore.config?.streamMode
  if (!mode) return ''
  return `${mode.width}x${mode.height}@${mode.framerate}fps Quality: ${mode.quality}`
})

async function loadVideoModes() {
  if (!streamEnabled.value) {
    streamModeOptions.value = []
    return
  }

  try {
    const modes = await videoApi.getVideoModes()
    streamModeOptions.value = modes.map(mode => ({
      text: `${mode.width}x${mode.height} MJPEG ${mode.framerate} fps`,
      value: mode,
    }))

    // Set the initial selected mode from the config
    if (configStore.config?.streamMode) {
      const matchingOption = streamModeOptions.value.find(
        opt =>
          opt.value.width === configStore.config!.streamMode.width &&
          opt.value.height === configStore.config!.streamMode.height &&
          opt.value.framerate === configStore.config!.streamMode.framerate
      )
      if (matchingOption) {
        selectedStreamMode.value = matchingOption.value
      } else {
        // If the configured mode is not in the list, use it directly
        selectedStreamMode.value = configStore.config.streamMode
      }
    }
  } catch (error) {
    console.error('Failed to load stream modes:', error)
    streamModeOptions.value = [] // Clear options on error
  }
}

function applySettings() {
  configStore.updateStreamMode({ ...selectedStreamMode.value, quality: selectedStreamQuality.value })
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

watch(streamEnabled, (newValue, oldValue) => {
  if (newValue && !oldValue) {
    loadVideoModes()
  }
})

onMounted(() => {
  loadVideoModes() // Load modes if stream is already enabled on mount

  if (configStore.config?.streamMode.quality) {
    selectedStreamQuality.value = configStore.config.streamMode.quality
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
