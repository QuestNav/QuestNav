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
            <label for="stream-resolution">Resolution:</label>
            <select id="stream-resolution" v-model="selectedResolution">
              <option v-for="option in resolutionOptions" :key="option.text" :value="option.value">
                {{ option.text }}
              </option>
            </select>
          </div>
          <div class="control-container">
            <label for="stream-framerate">FPS:</label>
            <select id="stream-framerate" v-model="selectedFramerate">
              <option v-for="rate in framerateOptions" :key="rate" :value="rate">{{ rate }}</option>
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
import { videoApi } from '../api/video'

const configStore = useConfigStore()
const cameraContainer = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

const streamEnabled = computed(() => configStore.config?.enablePassthroughStream ?? false)
const highQualityStreamEnabled = computed(() => configStore.config?.enableHighQualityStream ?? false)

const allResolutionOptions = ref<{ text: string; value: { width: number; height: number } }[]>([])
const resolutionOptions = computed(() => {
  if (highQualityStreamEnabled.value) {
    return allResolutionOptions.value
  }
  return allResolutionOptions.value.filter(opt => opt.value.width * opt.value.height <= 307200) // 640 * 480
})

const allFramerateOptions = ref<number[]>([])
const framerateOptions = computed(() => {
  if (highQualityStreamEnabled.value) {
    return allFramerateOptions.value
  }
  return allFramerateOptions.value.filter(rate => rate <= 30)
})

const selectedResolution = ref({ width: 320, height: 240 })
const selectedFramerate = ref(24)
const selectedStreamQuality = ref(75)
const cacheBuster = ref(Date.now())

const streamUrl = computed(() => `${videoApi.baseUrl}/video?t=${cacheBuster.value}`)

const activeStreamSettings = computed(() => {
  const mode = configStore.config?.streamMode
  if (!mode) return ''
  return `${mode.width}x${mode.height}@${mode.framerate}fps Quality: ${mode.quality}`
})

function syncResolutionSelection() {
  // Try to re-sync with the configured resolution
  if (configStore.config?.streamMode) {
    const { width, height } = configStore.config.streamMode
    const matchingOption = resolutionOptions.value.find(
      opt => opt.value.width === width && opt.value.height === height
    )

    if (matchingOption) {
      // If the configured resolution is valid for the current settings, apply it
      selectedResolution.value = matchingOption.value
      return
    }
  }

  // If the configured resolution is not available or doesn't exist,
  // check if the current selection is still valid in the filtered list.
  const currentSelectionValid = resolutionOptions.value.some(
    opt =>
      opt.value.width === selectedResolution.value.width && opt.value.height === selectedResolution.value.height
  )

  // If the current selection is no longer valid (e.g., high quality was turned off)
  // and there are options available, default to the first one.
  if (!currentSelectionValid && resolutionOptions.value.length > 0) {
    selectedResolution.value = resolutionOptions.value[0].value
  }
}

function syncFramerateSelection() {
  // Try to re-sync with the configured framerate
  if (configStore.config?.streamMode) {
    const { framerate } = configStore.config.streamMode
    const matchingOption = framerateOptions.value.find(rate => rate === framerate)

    if (matchingOption) {
      // If the configured framerate is valid for the current settings, apply it
      selectedFramerate.value = matchingOption
      return
    }
  }

  // If the configured framerate is not available or doesn't exist,
  // check if the current selection is still valid in the filtered list.
  const currentSelectionValid = framerateOptions.value.includes(selectedFramerate.value)

  // If the current selection is no longer valid (e.g., high quality was turned off)
  // and there are options available, default to the first one.
  if (!currentSelectionValid && framerateOptions.value.length > 0) {
    selectedFramerate.value = framerateOptions.value[0]
  }
}

async function loadVideoModes() {
  if (!streamEnabled.value) {
    allResolutionOptions.value = []
    return
  }

  try {
    const modes = await videoApi.getVideoModes()
    const uniqueResolutions = [...new Map(modes.map(m => [`${m.width}x${m.height}`, m])).values()]
    const uniqueFramerates = [...new Set(modes.map(m => m.framerate))].sort((a, b) => a - b)

    allResolutionOptions.value = uniqueResolutions.map(mode => ({
      text: `${mode.width}x${mode.height}`,
      value: { width: mode.width, height: mode.height },
    }))
    allFramerateOptions.value = uniqueFramerates

    syncResolutionSelection()
    syncFramerateSelection()
  } catch (error) {
    console.error('Failed to load stream modes:', error)
    // Clear options on error
    allResolutionOptions.value = []
    allFramerateOptions.value = []
  }
}

function applySettings() {
  configStore.updateStreamMode({
    width: selectedResolution.value.width,
    height: selectedResolution.value.height,
    framerate: selectedFramerate.value,
    quality: selectedStreamQuality.value,
  })
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

watch(highQualityStreamEnabled, () => {
  // When the high quality setting changes, the available options change, so we must re-sync.
  syncResolutionSelection()
  syncFramerateSelection()
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
