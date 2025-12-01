<template>
  <div class="config-form">
    <!-- Loading State -->
    <div v-if="configStore.isLoading" class="loading-container">
      <div class="spinner"></div>
      <p>Loading configuration...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="configStore.error" class="error-container card">
      <h3>⚠️ Error</h3>
      <p>{{ configStore.error }}</p>
      <button @click="loadData">Retry</button>
    </div>

    <!-- Configuration Form with Tabs -->
    <div v-else-if="configStore.config" class="config-content">
      <!-- Tab Navigation -->
      <div class="tabs-container card">
        <div class="tabs-nav">
          <button
            v-for="tab in tabs"
            :key="tab"
            :class="['tab-button', { active: activeTab === tab }]"
            @click="activeTab = tab"
          >
            {{ tab }}
          </button>
        </div>

        <!-- Tab Content -->
        <div class="tab-content">
          <!-- Status Tab -->
          <div v-show="activeTab === 'Status'" class="tab-panel">
            <StatusView />
          </div>
          
          <!-- Logs Tab -->
          <div v-show="activeTab === 'Logs'" class="tab-panel">
            <LogsView />
          </div>
          
          <!-- Settings Tab -->
          <div v-show="activeTab === 'Settings'" class="tab-panel">
            <div class="settings-grid">
              <!-- Team Number -->
              <div class="config-field" :class="{ 'field-override-active': isDebugIPActive }">
                <div class="field-header">
                  <label class="field-label">
                    Team Number
                    <span v-if="isDebugIPActive" class="override-badge">OVERRIDDEN</span>
                  </label>
                  <span class="field-description">
                    <template v-if="isDebugIPActive">
                      <strong>Enter a team number and click Apply to clear the IP override</strong>
                    </template>
                    <template v-else>
                      FRC team number (1-25599)
                    </template>
                  </span>
                </div>
                <div class="field-control input-control">
                  <input
                    type="number"
                    :value="pendingTeamNumber ?? configStore.config.teamNumber"
                    @input="handleTeamNumberInput"
                    @keyup.enter="submitTeamNumber"
                    min="1"
                    max="25599"
                  />
                  <button 
                    v-if="hasTeamNumberChanged"
                    @click="submitTeamNumber"
                    class="submit-button"
                  >
                    Apply
                  </button>
                </div>
              </div>

              <!-- Debug IP Override -->
              <div class="config-field" :class="{ 'field-warning': isDebugIPActive }">
                <div class="field-header">
                  <label class="field-label">
                    Debug IP Override
                    <span v-if="isDebugIPActive" class="debug-badge">DEBUG MODE</span>
                  </label>
                  <span class="field-description">Override robot IP for testing (leave empty for team number)</span>
                </div>
                <div class="field-control input-control">
                  <input
                    type="text"
                    :value="pendingDebugIP ?? configStore.config.debugIpOverride"
                    @input="handleDebugIPInput"
                    @keyup.enter="submitDebugIP"
                    placeholder="e.g., 10.0.0.2"
                  />
                  <button 
                    v-if="hasDebugIPChanged"
                    @click="submitDebugIP"
                    class="submit-button"
                  >
                    Apply
                  </button>
                </div>
              </div>

              <!-- Auto Start on Boot -->
              <div class="config-field">
                <div class="field-header">
                  <label class="field-label">Auto Start on Boot</label>
                  <span class="field-description">Start QuestNav when headset boots</span>
                </div>
                <div class="field-control checkbox-control">
                  <input
                    type="checkbox"
                    :checked="configStore.config.enableAutoStartOnBoot"
                    @change="handleAutoStartChange"
                  />
                  <span class="checkbox-label">{{ configStore.config.enableAutoStartOnBoot ? 'Enabled' : 'Disabled' }}</span>
                </div>
              </div>

              <!-- Debug Logging -->
              <div class="config-field">
                <div class="field-header">
                  <label class="field-label">Debug Logging</label>
                  <span class="field-description">Enable verbose debug logging</span>
                </div>
                <div class="field-control checkbox-control">
                  <input
                    type="checkbox"
                    :checked="configStore.config.enableDebugLogging"
                    @change="handleDebugLoggingChange"
                  />
                  <span class="checkbox-label">{{ configStore.config.enableDebugLogging ? 'Enabled' : 'Disabled' }}</span>
                </div>
              </div>

              <!-- Reset to Defaults -->
              <div class="config-field reset-field">
                <div class="field-header">
                  <label class="field-label">Reset Configuration</label>
                  <span class="field-description">Reset all settings to defaults</span>
                </div>
                <div class="field-control">
                  <button @click="handleReset" class="reset-button">Reset to Defaults</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="empty-container card">
      <p>No configuration available</p>
      <button @click="loadData">Load Configuration</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useConfigStore } from '../stores/config'
import StatusView from './StatusView.vue'
import LogsView from './LogsView.vue'

const configStore = useConfigStore()
const activeTab = ref<string>('Status')
const tabs = ['Status', 'Logs', 'Settings']
let pollInterval: number | null = null

const pendingTeamNumber = ref<number | null>(null)
const pendingDebugIP = ref<string | null>(null)

const isDebugIPActive = computed(() => {
  return configStore.config?.debugIpOverride && configStore.config.debugIpOverride.length > 0
})

const hasTeamNumberChanged = computed(() => {
  return pendingTeamNumber.value !== null && pendingTeamNumber.value !== configStore.config?.teamNumber
})

const hasDebugIPChanged = computed(() => {
  return pendingDebugIP.value !== null && pendingDebugIP.value !== configStore.config?.debugIpOverride
})

onMounted(async () => {
  await loadData()
  
  pollInterval = setInterval(async () => {
    await configStore.loadConfig(false)
    if (configStore.config) {
      if (pendingTeamNumber.value === configStore.config.teamNumber) pendingTeamNumber.value = null
      if (pendingDebugIP.value === configStore.config.debugIpOverride) pendingDebugIP.value = null
    }
  }, 3000) as unknown as number
})

onUnmounted(() => {
  if (pollInterval !== null) clearInterval(pollInterval)
})

async function loadData() {
  await configStore.loadConfig()
}

function handleTeamNumberInput(event: Event) {
  const target = event.target as HTMLInputElement
  const value = parseInt(target.value)
  if (!isNaN(value)) pendingTeamNumber.value = value
}

async function submitTeamNumber() {
  if (pendingTeamNumber.value !== null) {
    await configStore.updateTeamNumber(pendingTeamNumber.value)
    pendingTeamNumber.value = null
  }
}

function handleDebugIPInput(event: Event) {
  const target = event.target as HTMLInputElement
  pendingDebugIP.value = target.value
}

async function submitDebugIP() {
  if (pendingDebugIP.value !== null) {
    await configStore.updateDebugIpOverride(pendingDebugIP.value)
    pendingDebugIP.value = null
  }
}

async function handleAutoStartChange(event: Event) {
  const target = event.target as HTMLInputElement
  await configStore.updateEnableAutoStartOnBoot(target.checked)
}

async function handleDebugLoggingChange(event: Event) {
  const target = event.target as HTMLInputElement
  await configStore.updateEnableDebugLogging(target.checked)
}

async function handleReset() {
  if (confirm('Reset all settings to defaults?')) {
    await configStore.resetToDefaults()
  }
}
</script>

<style scoped>
.config-form {
  width: 100%;
  max-width: 1400px;
  margin: 0 auto;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 4rem 2rem;
  gap: 1.5rem;
}

.error-container, .empty-container {
  padding: 3rem 2rem;
  text-align: center;
}

.error-container h3 {
  color: var(--danger-color);
  margin-bottom: 1rem;
}

.tabs-container {
  padding: 0;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  background: var(--bg-tertiary);
  border-bottom: 2px solid var(--border-color);
}

.tab-button {
  flex: 1;
  padding: 1rem 1.5rem;
  background: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--text-secondary);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.tab-button:hover {
  background: var(--card-bg);
  color: var(--primary-color);
}

.tab-button.active {
  color: var(--text-primary);
  background: var(--card-bg);
  border-bottom-color: var(--primary-color);
}

.tab-content {
  padding: 2rem;
  min-height: 400px;
}

.settings-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.config-field {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
}

.config-field.field-warning {
  border-color: var(--warning-color);
  background: rgba(255, 193, 7, 0.1);
}

.config-field.field-override-active {
  border-color: var(--warning-color);
  background: rgba(255, 193, 7, 0.15);
}

.config-field.field-override-active .field-description {
  color: var(--warning-color);
}

.field-header {
  margin-bottom: 1rem;
}

.field-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.field-description {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.debug-badge, .override-badge {
  font-size: 0.7rem;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-weight: 700;
}

.debug-badge {
  background: var(--danger-color);
  color: white;
}

.override-badge {
  background: var(--text-secondary);
  color: white;
}

.input-control {
  display: flex;
  gap: 0.5rem;
}

.input-control input {
  flex: 1;
  padding: 0.75rem;
  border: 1px solid var(--border-color);
  border-radius: 6px;
  font-size: 1rem;
}

.submit-button {
  padding: 0.75rem 1.25rem;
  background: var(--primary-color);
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

.checkbox-control {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.checkbox-control input {
  width: 1.25rem;
  height: 1.25rem;
}

.checkbox-label {
  font-weight: 500;
}

.reset-field {
  grid-column: 1 / -1;
}

.reset-button {
  padding: 0.75rem 1.5rem;
  background: var(--danger-color);
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

@media (max-width: 768px) {
  .settings-grid {
    grid-template-columns: 1fr;
  }
}
</style>

