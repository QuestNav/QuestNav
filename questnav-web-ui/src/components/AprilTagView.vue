<template>
  <div class="settings-grid">
    <!-- AprilTag Detector Enable/Disable -->
    <ConfigField title="AprilTag Detector" description="Enable AprilTag detection for pose estimation"
                 control-class="checkbox-control">
      <label class="checkbox-label">
        <input type="checkbox" :checked="configStore.config?.enableAprilTagDetector ?? false"
               @change="handleDetectorEnabledChange" />
        {{ configStore.config?.enableAprilTagDetector ? 'Enabled' : 'Disabled' }}
      </label>
    </ConfigField>

    <!-- Field Layout (restart-on-change). The dropdown writes the new selection to
         config immediately, but the running app keeps using the previously-loaded
         layout until it restarts. The banner below the grid prompts the user when a
         restart is pending. The "Manage Custom Layouts" button opens a modal where
         the user can paste / edit / rename / delete their own JSONs. -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Field Layout"
                 description="AprilTag layout for the field. Changes take effect after restart."
                 control-class="input-control">
      <template #badge>
        <span v-if="fieldLayoutDirty" class="dirty-badge">●</span>
      </template>
      <div style="display: flex; gap: 0.5rem; align-items: center; width: 100%;">
        <select :value="pendingFieldLayoutFile" @change="handleFieldLayoutChange"
                :disabled="fieldLayoutOptions.length === 0" style="flex: 1;">
          <option v-if="fieldLayoutOptions.length === 0" :value="pendingFieldLayoutFile">Loading...</option>
          <option v-for="opt in fieldLayoutOptions" :key="opt.fileName" :value="opt.fileName">
            {{ opt.displayName }} ({{ opt.tagCount }} tags{{ opt.source === 'custom' ? ', custom' : '' }})
          </option>
        </select>
        <button @click="showFieldLayoutManager = true" type="button" class="cancel-button">
          Manage Custom...
        </button>
      </div>
    </ConfigField>

    <!-- Camera Resolution
         This dropdown actually configures the underlying Meta SDK PassthroughCameraAccess.
         Quest 3 / Quest 3S support 1280x960 and 1280x1280 only; the list is sourced from
         the SDK at runtime. Higher pixel count = larger detection range but more CPU and
         battery. Note: when the AprilTag detector is enabled, this resolution also drives
         the passthrough video stream (see camera arbiter / "Locked by AprilTag" badge in
         the Camera tab).
         Note: ANCHOR_ENHANCED detection mode is not implemented yet (it throws on the
         backend). The mode dropdown is intentionally hidden; the only supported value
         (TRADITIONAL = 0) is sent automatically by submitModeSettings(). -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Camera Resolution"
                 description="Headset camera resolution. Higher resolutions detect tags from further away but cost more CPU and battery. While AprilTag is enabled, this resolution overrides the passthrough stream's resolution."
                 control-class="input-control">
      <template #badge>
        <span v-if="isResolutionFieldDirty" class="dirty-badge">●</span>
      </template>
      <select :value="resolutionKey" @change="handleResolutionChange" :disabled="resolutionOptions.length === 0">
        <option v-if="resolutionOptions.length === 0" :value="resolutionKey">Loading...</option>
        <option v-for="opt in resolutionOptions" :key="opt.key" :value="opt.key">
          {{ opt.label }}
        </option>
      </select>
    </ConfigField>

    <!-- Detection Framerate. The Meta SDK does not let us set a camera framerate;
         frames arrive continuously at ~60 Hz. This dropdown controls how often the
         AprilTag detection coroutine pulls a frame and runs the detector, which lets
         a team trade off CPU/battery for correction frequency. -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Detection Framerate"
                 description="How often to run AprilTag detection on captured frames. The camera always runs at 60 Hz; this only affects how many frames per second the detector processes. Lower = less CPU and battery, fewer pose corrections."
                 control-class="input-control">
      <template #badge>
        <span v-if="isFramerateFieldDirty" class="dirty-badge">●</span>
      </template>
      <select :value="pendingMode.framerate" @change="handleFramerateChange" :disabled="framerateOptions.length === 0">
        <option v-if="framerateOptions.length === 0" :value="pendingMode.framerate">Loading...</option>
        <option v-for="fps in framerateOptions" :key="fps" :value="fps">{{ fps }} fps</option>
      </select>
    </ConfigField>

    <!-- Detection Range -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Detection Range"
                 description="Maximum distance for tag detection" control-class="input-control">
      <template #badge>
        <span v-if="isMaxDistanceFieldDirty" class="dirty-badge">●</span>
      </template>
      <input type="range" :value="pendingMode.maxDistance" @input="handleMaxDistanceChange" min="0.5" max="10"
             step="0.1" style="flex: 2;" />
      <span class="range-value">{{ pendingMode.maxDistance }}m</span>
    </ConfigField>

    <!-- Minimum Tags -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Minimum Tags Required"
                 description="Minimum number of tags needed before a pose update is published. Higher values reject noisier observations but reduce update rate."
                 control-class="input-control">
      <template #badge>
        <span v-if="isMinTagsFieldDirty" class="dirty-badge">●</span>
      </template>
      <select :value="pendingMode.minimumNumberOfTags" @change="handleMinimumTagsChange">
        <option v-for="n in MIN_TAGS_OPTIONS" :key="n" :value="n">{{ n }}</option>
      </select>
    </ConfigField>

    <!-- Ignored Tag IDs (blacklist) -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Ignored Tag IDs"
                 description="Comma-separated list of AprilTag IDs to ignore (leave empty to detect all)" control-class="input-control">
      <template #badge>
        <span v-if="isIgnoredIdsFieldDirty" class="dirty-badge">●</span>
      </template>
      <div style="display: flex; gap: 0.5rem; align-items: center; width: 100%;">
        <input type="text" :value="ignoredIdsText" @input="handleIgnoredIdsChange" placeholder="e.g., 16,17" style="flex: 1;" />
        <button @click="clearIgnoredIds" :disabled="pendingMode.ignoredIds.length === 0" class="cancel-button" type="button">
          Clear
        </button>
      </div>
    </ConfigField>
  </div>

  <!-- Apply Button Section -->
  <div v-if="configStore.config?.enableAprilTagDetector">
    <div class="apply-buttons">
      <button @click="submitModeSettings" :disabled="!hasModeChanged" class="submit-button primary">
        Apply
      </button>
      <button @click="cancelChanges" :disabled="!hasModeChanged" class="cancel-button">
        Cancel
      </button>
    </div>
  </div>

  <!-- Restart-required banner. Field layout is read once at startup; if the user has
       saved a new selection that does not match the active layout, prompt them to
       restart. The button calls POST /api/restart (existing endpoint). -->
  <div v-if="restartRequired" class="restart-banner">
    <span class="restart-icon">⟳</span>
    <span class="restart-text">
      Field layout change saved. Restart QuestNav to apply
      <strong>{{ savedFieldLayoutFile }}</strong>.
    </span>
    <button @click="handleRestart" class="restart-button">Restart App</button>
  </div>

  <!-- Custom field layout manager modal. Opens via the "Manage Custom..." button next
       to the field-layout dropdown. Refreshes the dropdown on save / rename / delete
       via the @changed event. -->
  <FieldLayoutManager v-if="showFieldLayoutManager"
                      @close="showFieldLayoutManager = false"
                      @changed="loadFieldLayouts" />
</template>

<script setup lang="ts">
import { ref, computed, watch, inject, onMounted } from 'vue'
import { useConfigStore } from '../stores/config'
import { configApi } from '../api/config'
import { videoApi } from '../api/video'
import type { AprilTagDetectorMode, AprilTagFieldLayoutEntry, VideoModeModel } from '../types'
import ConfigField from './ConfigField.vue'
import FieldLayoutManager from './FieldLayoutManager.vue'

// Mirrors QuestNavConstants.AprilTag.MINIMUM_TAGS_OPTIONS on the server. Server-side
// validation rejects values outside this set so keep these in sync.
const MIN_TAGS_OPTIONS = [1, 2, 3, 4]

// Use mock store if available (for testing), otherwise use real store
const injectedStore = inject('configStore', null)
const configStore = injectedStore || (window as any).__MOCK_CONFIG_STORE__ || useConfigStore()

// Pending changes for batch updates
const pendingMode = ref<AprilTagDetectorMode>({
  mode: 0,
  width: 640,
  height: 480,
  framerate: 30,
  ignoredIds: [],
  maxDistance: 4.0,
  minimumNumberOfTags: 2,
  fieldLayoutFile: '2026-rebuilt-welded.json'
})

// FRC tag36h11 IDs in current use never exceed ~40; clamp to 50 for headroom.
const FRC_MAX_TAG_ID = 50

// Track which specific fields the user has actually modified
const userModifiedFields = ref<Set<string>>(new Set())

// Initialize pending mode when config loads
watch(() => configStore.config?.aprilTagDetectorMode, (newMode) => {
  if (newMode) {
    // On background refresh, only update fields that user hasn't modified
    if (userModifiedFields.value.size === 0) {
      // No user modifications, safe to update everything
      pendingMode.value = { ...newMode }
    } else {
      // Selectively update only non-user-modified fields
      const updated = { ...pendingMode.value }

      if (!userModifiedFields.value.has('mode')) updated.mode = newMode.mode
      if (!userModifiedFields.value.has('width')) updated.width = newMode.width
      if (!userModifiedFields.value.has('height')) updated.height = newMode.height
      if (!userModifiedFields.value.has('framerate')) updated.framerate = newMode.framerate
      if (!userModifiedFields.value.has('maxDistance')) updated.maxDistance = newMode.maxDistance
      if (!userModifiedFields.value.has('minimumNumberOfTags')) updated.minimumNumberOfTags = newMode.minimumNumberOfTags
      if (!userModifiedFields.value.has('ignoredIds')) updated.ignoredIds = [...newMode.ignoredIds]
      if (!userModifiedFields.value.has('fieldLayoutFile')) updated.fieldLayoutFile = newMode.fieldLayoutFile

      pendingMode.value = updated
    }
  }
}, { immediate: true })

// Field layout dropdown state. Loaded from /api/apriltag-field-layouts on mount.
const fieldLayoutOptions = ref<AprilTagFieldLayoutEntry[]>([])

// Toggles the "Custom Field Layouts" manager modal. The modal handles its own
// loading state and emits 'changed' after save/rename/delete so we can refresh
// the dropdown immediately.
const showFieldLayoutManager = ref(false)

// pendingFieldLayoutFile is what the user has chosen in the dropdown (may not yet be saved
// to the backend). pendingMode.fieldLayoutFile is what's currently in pendingMode.
// They diverge between change and Apply when other dirty fields are also pending.
const pendingFieldLayoutFile = computed({
  get: () => pendingMode.value.fieldLayoutFile,
  set: (value: string) => {
    pendingMode.value.fieldLayoutFile = value
  }
})

const fieldLayoutDirty = computed(() => userModifiedFields.value.has('fieldLayoutFile'))

// savedFieldLayoutFile reflects the field-layout file currently persisted in the backend.
// restartRequired is true when the saved value differs from what's in pendingMode at
// Apply time. We surface a banner + Restart App button so the user can apply the change.
const savedFieldLayoutFile = computed(() => configStore.config?.aprilTagDetectorMode?.fieldLayoutFile ?? '')
// Active layout: lock in the value at first load; the running app cannot change layouts
// without a restart so this is the right comparison target for the restart banner.
const activeFieldLayoutFile = ref<string | null>(null)
const restartRequired = computed(() => {
  if (activeFieldLayoutFile.value === null) return false
  if (!savedFieldLayoutFile.value) return false
  return savedFieldLayoutFile.value !== activeFieldLayoutFile.value
})

// Computed for ignored IDs text representation. Clamps every entry to [0, FRC_MAX_TAG_ID]
// and de-duplicates so the user can paste freely without breaking the backend filter.
const ignoredIdsText = computed({
  get: () => pendingMode.value.ignoredIds.join(','),
  set: (value: string) => {
    const ids = Array.from(new Set(
      value.split(',')
        .map(id => parseInt(id.trim()))
        .filter(id => !isNaN(id) && id >= 0 && id <= FRC_MAX_TAG_ID)
    ))
    pendingMode.value.ignoredIds = ids
    // Don't auto-mark as modified here - let the change handler do it
  }
})

// Check if mode settings have changed
const hasModeChanged = computed(() => {
  return userModifiedFields.value.size > 0
})

// Individual field dirty state indicators (only show dirty if user actually modified them)
// Resolution badge covers width AND height (both flip together); FPS gets its own badge.
const isResolutionFieldDirty = computed(() => {
  return userModifiedFields.value.has('width') || userModifiedFields.value.has('height')
})

const isFramerateFieldDirty = computed(() => {
  return userModifiedFields.value.has('framerate')
})

const isMaxDistanceFieldDirty = computed(() => {
  return userModifiedFields.value.has('maxDistance')
})

const isMinTagsFieldDirty = computed(() => {
  return userModifiedFields.value.has('minimumNumberOfTags')
})

const isIgnoredIdsFieldDirty = computed(() => {
  return userModifiedFields.value.has('ignoredIds')
})

// Event handlers
async function handleDetectorEnabledChange(event: Event) {
  const target = event.target as HTMLInputElement
  await configStore.updateEnableAprilTagDetector(target.checked)
}

// Available video modes for AprilTag detection, loaded once on mount from
// /api/apriltag-video-modes. This is the resolution x framerate cross-product the
// underlying camera actually supports (with an editor-mode fallback on the server side).
const apriltagVideoModes = ref<VideoModeModel[]>([])

// Unique resolutions derived from the supported modes, formatted for the dropdown.
const resolutionOptions = computed(() => {
  const seen = new Map<string, { key: string; label: string; width: number; height: number }>()
  for (const m of apriltagVideoModes.value) {
    const key = `${m.width}x${m.height}`
    if (!seen.has(key)) {
      seen.set(key, { key, label: key, width: m.width, height: m.height })
    }
  }
  return Array.from(seen.values())
})

// FPS options filtered to the current resolution. Different resolutions can support
// different FPS values; resyncing the framerate selection happens in
// syncResolutionAndFramerate() whenever the modes list or pending resolution changes.
const framerateOptions = computed(() => {
  const fps = new Set<number>()
  for (const m of apriltagVideoModes.value) {
    if (m.width === pendingMode.value.width && m.height === pendingMode.value.height) {
      fps.add(m.framerate)
    }
  }
  return Array.from(fps).sort((a, b) => a - b)
})

const resolutionKey = computed(() => `${pendingMode.value.width}x${pendingMode.value.height}`)

function handleResolutionChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const opt = resolutionOptions.value.find(o => o.key === target.value)
  if (!opt) return
  pendingMode.value.width = opt.width
  pendingMode.value.height = opt.height
  userModifiedFields.value.add('width')
  userModifiedFields.value.add('height')
  // The chosen FPS may not be valid for the new resolution. If so, snap to the closest.
  const validFps = apriltagVideoModes.value
    .filter(m => m.width === opt.width && m.height === opt.height)
    .map(m => m.framerate)
  if (validFps.length > 0 && !validFps.includes(pendingMode.value.framerate)) {
    const closest = validFps.reduce((best, fps) =>
      Math.abs(fps - pendingMode.value.framerate) < Math.abs(best - pendingMode.value.framerate) ? fps : best
    )
    pendingMode.value.framerate = closest
    userModifiedFields.value.add('framerate')
  }
}

function handleFramerateChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const value = parseInt(target.value)
  if (!isNaN(value)) {
    pendingMode.value.framerate = value
    userModifiedFields.value.add('framerate')
  }
}

function handleMaxDistanceChange(event: Event) {
  const target = event.target as HTMLInputElement
  const value = parseFloat(target.value)
  if (!isNaN(value)) {
    pendingMode.value.maxDistance = value
    userModifiedFields.value.add('maxDistance')
  }
}

function handleMinimumTagsChange(event: Event) {
  const target = event.target as HTMLSelectElement
  const value = parseInt(target.value)
  if (!isNaN(value) && MIN_TAGS_OPTIONS.includes(value)) {
    pendingMode.value.minimumNumberOfTags = value
    userModifiedFields.value.add('minimumNumberOfTags')
  }
}

function handleIgnoredIdsChange(event: Event) {
  const target = event.target as HTMLInputElement
  ignoredIdsText.value = target.value
  userModifiedFields.value.add('ignoredIds')
}

function handleFieldLayoutChange(event: Event) {
  const target = event.target as HTMLSelectElement
  pendingMode.value.fieldLayoutFile = target.value
  userModifiedFields.value.add('fieldLayoutFile')
}

async function loadFieldLayouts() {
  try {
    fieldLayoutOptions.value = await configApi.getAprilTagFieldLayouts()
  } catch (err) {
    console.error('Failed to load AprilTag field layouts:', err)
    fieldLayoutOptions.value = []
  }
}

async function loadAprilTagVideoModes() {
  try {
    apriltagVideoModes.value = await videoApi.getAprilTagVideoModes()
  } catch (err) {
    console.error('Failed to load AprilTag video modes:', err)
    apriltagVideoModes.value = []
  }
}

async function handleRestart() {
  if (!confirm('Restart QuestNav now to apply the new field layout?')) {
    return
  }
  try {
    await configApi.restartApp()
  } catch (err) {
    alert('Failed to restart: ' + (err instanceof Error ? err.message : String(err)))
  }
}

onMounted(async () => {
  await Promise.all([loadFieldLayouts(), loadAprilTagVideoModes()])
  // Snapshot the saved field-layout value at first mount; this is treated as the
  // "currently active" layout for the running app session. Subsequent changes that
  // diverge from this snapshot trigger the restart-required banner.
  if (configStore.config?.aprilTagDetectorMode?.fieldLayoutFile) {
    activeFieldLayoutFile.value = configStore.config.aprilTagDetectorMode.fieldLayoutFile
  } else {
    // Wait for config to load, then snapshot.
    const stop = watch(() => configStore.config?.aprilTagDetectorMode?.fieldLayoutFile, (v) => {
      if (v) {
        activeFieldLayoutFile.value = v
        stop()
      }
    })
  }
})

function clearIgnoredIds() {
  pendingMode.value.ignoredIds = []
  userModifiedFields.value.add('ignoredIds')
}

async function submitModeSettings() {
  await configStore.updateAprilTagDetectorMode(pendingMode.value)
  userModifiedFields.value.clear()
}

function cancelChanges() {
  const current = configStore.config?.aprilTagDetectorMode
  if (current) {
    pendingMode.value = { ...current }
    userModifiedFields.value.clear()
  }
}
</script>

<style scoped>
.dirty-badge {
  color: #ff9500;
  font-size: 14px;
  margin-left: 6px;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0% {
    opacity: 1;
  }

  50% {
    opacity: 0.6;
  }

  100% {
    opacity: 1;
  }
}

.apply-buttons {
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding-top: 1.5rem;
  padding-right: 0.5rem;
}

.submit-button,
.cancel-button {
  padding: 0.5rem 1rem;
  border-radius: 4px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  border: 1px solid transparent;
}

.submit-button:disabled,
.cancel-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.submit-button.primary {
  background: var(--primary-color);
  color: white;
  border-color: var(--primary-color);
}

.submit-button.primary:hover:not(:disabled) {
  background: var(--primary-dark);
  border-color: var(--primary-dark);
}

.cancel-button {
  background: var(--secondary-color);
  color: white;
  border-color: var(--secondary-color);
}

.cancel-button:hover:not(:disabled) {
  background: var(--text-secondary);
  border-color: var(--text-secondary);
}

.changes-summary {
  color: var(--text-secondary);
  font-style: italic;
}

.range-value {
  min-width: 40px;
  text-align: center;
  font-weight: 500;
  color: var(--text-primary);
  font-size: 0.9rem;
}

.restart-banner {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(255, 193, 7, 0.12);
  border: 1px solid var(--warning-color, #ffc107);
  color: var(--text-primary);
  border-radius: 6px;
  padding: 0.75rem 1rem;
  margin-top: 1rem;
  font-size: 0.9rem;
  font-weight: 500;
}

.restart-banner .restart-icon {
  font-size: 1.25rem;
  flex-shrink: 0;
}

.restart-banner .restart-text {
  flex: 1;
}

.restart-button {
  padding: 0.4rem 0.9rem;
  border-radius: 4px;
  font-weight: 600;
  background: var(--warning-color, #ffc107);
  color: #000;
  border: 1px solid var(--warning-color, #ffc107);
  cursor: pointer;
  transition: opacity 0.2s ease;
}

.restart-button:hover {
  opacity: 0.85;
}
</style>