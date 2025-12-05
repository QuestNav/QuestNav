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
              <ConfigField
                title="Team Number"
                :field-class="{ 'field-override-active': isDebugIPActive }"
                control-class="input-control"
              >
                <template #badge>
                  <span v-if="isDebugIPActive" class="override-badge">OVERRIDDEN</span>
                </template>
                <template #description>
                  <template v-if="isDebugIPActive">
                    <strong>Enter a team number and click Apply to clear the IP override</strong>
                  </template>
                  <template v-else>
                    FRC team number (1-25599)
                  </template>
                </template>
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
              </ConfigField>

              <!-- Debug IP Override -->
              <ConfigField
                title="Debug IP Override"
                description="Override robot IP for testing (leave empty for team number)"
                :field-class="{ 'field-warning': isDebugIPActive }"
                control-class="input-control"
              >
                <template #badge>
                  <span v-if="isDebugIPActive" class="debug-badge">DEBUG MODE</span>
                </template>
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
              </ConfigField>

              <!-- Auto Start on Boot -->
              <ConfigField
                title="Auto Start on Boot"
                description="Start QuestNav when headset boots"
                control-class="checkbox-control"
              >
                <input
                  type="checkbox"
                  :checked="configStore.config.enableAutoStartOnBoot"
                  @change="handleAutoStartChange"
                />
                <span class="checkbox-label">{{ configStore.config.enableAutoStartOnBoot ? 'Enabled' : 'Disabled' }}</span>
              </ConfigField>

              <!-- Debug Logging -->
              <ConfigField
                title="Debug Logging"
                description="Enable verbose debug logging"
                control-class="checkbox-control"
              >
                <input
                  type="checkbox"
                  :checked="configStore.config.enableDebugLogging"
                  @change="handleDebugLoggingChange"
                />
                <span class="checkbox-label">{{ configStore.config.enableDebugLogging ? 'Enabled' : 'Disabled' }}</span>
              </ConfigField>

              <!-- Reset to Defaults -->
              <ConfigField
                title="Reset Configuration"
                description="Reset all settings to defaults"
                field-class="reset-field"
              >
                <button @click="handleReset" class="reset-button">Reset to Defaults</button>
              </ConfigField>

              <!-- Database Management -->
              <ConfigField
                title="Database Management"
                description="Download or upload the configuration database"
                field-class="database-field"
                control-class="database-buttons"
              >
                <button @click="handleDownloadDatabase" class="database-button">Download Database</button>
                <label class="database-button upload-label">
                  Upload Database
                  <input type="file" accept=".db" @change="handleUploadDatabase" hidden />
                </label>
              </ConfigField>
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
import { configApi } from '../api/config'
import StatusView from './StatusView.vue'
import LogsView from './LogsView.vue'
import ConfigField from './ConfigField.vue'

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

async function handleDownloadDatabase() {
  try {
    await configApi.downloadDatabase()
  } catch (error) {
    alert('Failed to download database: ' + (error as Error).message)
  }
}

async function handleUploadDatabase(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return
  
  if (!confirm('Upload this database? The app will need to restart to apply changes.')) {
    target.value = ''
    return
  }
  
  try {
    const result = await configApi.uploadDatabase(file)
    alert(result.message)
  } catch (error) {
    alert('Failed to upload database: ' + (error as Error).message)
  }
  target.value = ''
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

.submit-button {
  padding: 0.75rem 1.25rem;
  background: var(--primary-color);
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

.checkbox-label {
  font-weight: 500;
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

.database-button {
  padding: 0.75rem 1.5rem;
  background: var(--card-bg);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  font-weight: 600;
  cursor: pointer;
}

.database-button:hover {
  background: var(--border-color);
}

.upload-label {
  display: inline-block;
}

@media (max-width: 768px) {
  .settings-grid {
    grid-template-columns: 1fr;
  }
}
</style>

