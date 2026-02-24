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

    <!-- Detection Mode -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Detection Mode" control-class="input-control">
      <template #description>
        <div><strong>Traditional</strong> - PnP solving</div>
        <div><strong>Anchor Enhanced</strong> - PnP solving + spatial anchors for enhanced performance</div>
      </template>
      <template #badge>
        <span v-if="isModeFieldDirty" class="dirty-badge">●</span>
      </template>
      <select :value="pendingMode.mode" @change="handleModeChange">
        <option value="0">Traditional</option>
        <option value="1">Anchor Enhanced</option>
      </select>
    </ConfigField>

    <!-- Detection Resolution -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Detection Resolution"
      description="Camera resolution for AprilTag detection (Width x Height @ Framerate)" control-class="input-control">
      <template #badge>
        <span v-if="isResolutionFieldDirty" class="dirty-badge">●</span>
      </template>
      <div style="display: flex; gap: 0.5rem; align-items: center;">
        <input type="text" :value="pendingMode.width" @input="handleWidthChange" style="width: 75px;"
          placeholder="Width" />
        <span>×</span>
        <input type="text" :value="pendingMode.height" @input="handleHeightChange" style="width: 75px;"
          placeholder="Height" />
        <span>@</span>
        <input type="text" :value="pendingMode.framerate" @input="handleFramerateChange" style="width: 75px;"
          placeholder="FPS" />
        <span>fps</span>
      </div>
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
      description="Minimum number of tags needed for pose estimation" control-class="input-control">
      <template #badge>
        <span v-if="isMinTagsFieldDirty" class="dirty-badge">●</span>
      </template>
      <input type="text" :value="pendingMode.minimumNumberOfTags" @input="handleMinimumTagsChange" />
    </ConfigField>

    <!-- Allowed Tag IDs -->
    <ConfigField v-if="configStore.config?.enableAprilTagDetector" title="Allowed Tag IDs"
      description="Comma-separated list of allowed AprilTag IDs (leave empty for all)" control-class="input-control">
      <template #badge>
        <span v-if="isAllowedIdsFieldDirty" class="dirty-badge">●</span>
      </template>
      <input type="text" :value="allowedIdsText" @input="handleAllowedIdsChange" placeholder="e.g., 1,2,3,4" />
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
</template>

<script setup lang="ts">
import { ref, computed, watch, inject } from 'vue'
import { useConfigStore } from '../stores/config'
import type { AprilTagDetectorMode } from '../types'
import ConfigField from './ConfigField.vue'

// Use mock store if available (for testing), otherwise use real store
const injectedStore = inject('configStore', null)
const configStore = injectedStore || (window as any).__MOCK_CONFIG_STORE__ || useConfigStore()

// Pending changes for batch updates
const pendingMode = ref<AprilTagDetectorMode>({
  mode: 0,
  width: 640,
  height: 480,
  framerate: 30,
  allowedIds: [],
  maxDistance: 4.0,
  minimumNumberOfTags: 2
})

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
      if (!userModifiedFields.value.has('allowedIds')) updated.allowedIds = [...newMode.allowedIds]

      pendingMode.value = updated
    }
  }
}, { immediate: true })

// Computed for allowed IDs text representation
const allowedIdsText = computed({
  get: () => pendingMode.value.allowedIds.join(','),
  set: (value: string) => {
    const ids = value.split(',')
      .map(id => parseInt(id.trim()))
      .filter(id => !isNaN(id) && id >= 0)
    pendingMode.value.allowedIds = ids
    // Don't auto-mark as modified here - let the change handler do it
  }
})

// Check if mode settings have changed
const hasModeChanged = computed(() => {
  return userModifiedFields.value.size > 0
})

// Individual field dirty state indicators (only show dirty if user actually modified them)
const isModeFieldDirty = computed(() => {
  return userModifiedFields.value.has('mode')
})

const isResolutionFieldDirty = computed(() => {
  return userModifiedFields.value.has('width') ||
    userModifiedFields.value.has('height') ||
    userModifiedFields.value.has('framerate')
})

const isMaxDistanceFieldDirty = computed(() => {
  return userModifiedFields.value.has('maxDistance')
})

const isMinTagsFieldDirty = computed(() => {
  return userModifiedFields.value.has('minimumNumberOfTags')
})

const isAllowedIdsFieldDirty = computed(() => {
  return userModifiedFields.value.has('allowedIds')
})

// Event handlers
async function handleDetectorEnabledChange(event: Event) {
  const target = event.target as HTMLInputElement
  await configStore.updateEnableAprilTagDetector(target.checked)
}

function handleModeChange(event: Event) {
  const target = event.target as HTMLSelectElement
  pendingMode.value.mode = parseInt(target.value)
  userModifiedFields.value.add('mode')
}

function handleWidthChange(event: Event) {
  const target = event.target as HTMLInputElement
  const value = parseInt(target.value)
  if (!isNaN(value) && value >= 160 && value <= 1920) {
    pendingMode.value.width = value
    userModifiedFields.value.add('width')
  }
}

function handleHeightChange(event: Event) {
  const target = event.target as HTMLInputElement
  const value = parseInt(target.value)
  if (!isNaN(value) && value >= 120 && value <= 1080) {
    pendingMode.value.height = value
    userModifiedFields.value.add('height')
  }
}

function handleFramerateChange(event: Event) {
  const target = event.target as HTMLInputElement
  const value = parseInt(target.value)
  if (!isNaN(value) && value >= 5 && value <= 60) {
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
  const target = event.target as HTMLInputElement
  const value = parseInt(target.value)
  if (!isNaN(value) && value >= 1 && value <= 8) {
    pendingMode.value.minimumNumberOfTags = value
    userModifiedFields.value.add('minimumNumberOfTags')
  }
}

function handleAllowedIdsChange(event: Event) {
  const target = event.target as HTMLInputElement
  allowedIdsText.value = target.value
  userModifiedFields.value.add('allowedIds')
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
</style>