<template>
  <div class="config-form">
    <!-- Restart Required Banner -->
    <transition name="slide-down">
      <div v-if="configStore.restartRequired" class="restart-banner">
        <div class="restart-content">
          <div class="restart-icon">⚠️</div>
          <div class="restart-message">
            <strong>Restart Required</strong>
            <p>Some settings require an application restart to take effect.</p>
          </div>
          <button @click="handleRestart" class="restart-button">
            Restart App
          </button>
        </div>
      </div>
    </transition>

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
    <div v-else-if="configStore.schema" class="config-content">
      <!-- Tab Navigation -->
      <div class="tabs-container card">
        <div class="tabs-nav">
          <button
            v-for="tab in allTabs"
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

          <!-- Calibration Tab -->
          <div v-show="activeTab === 'Calibration'" class="tab-panel">
            <CalibrationForm />
          </div>
          
          <!-- Config Tabs -->
          <div
            v-for="category in configStore.categories"
            :key="category"
            v-show="activeTab === category"
            class="tab-panel"
          >
            <div class="fields-grid">
              <ConfigField
                v-for="field in configStore.fieldsByCategory[category]"
                :key="field.path"
                :field="field"
                :value="configStore.values[field.path]"
                :debugIPOverride="configStore.values['WebServerConstants/debugNTServerAddressOverride']"
                @update="handleUpdate"
              />
            </div>

            <div v-if="category === 'Camera' && configStore.values['WebServerConstants/enablePassThrough'] === true" class="camera-stream">
              <img :src="'./video'" />
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
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useConfigStore } from '../stores/config'
import { useConnectionState } from '../composables/useConnectionState'
import { configApi } from '../api/config'
import ConfigField from './ConfigField.vue'
import StatusView from './StatusView.vue'
import LogsView from './LogsView.vue'
import CalibrationForm from './CalibrationForm.vue'

const configStore = useConfigStore()
const { isConnected } = useConnectionState()
const activeTab = ref<string>('Status')
let pollInterval: number | null = null

// Add Status, Logs, and Calibration as first tabs
const allTabs = computed(() => ['Status', 'Logs', 'Calibration', ...configStore.categories])

onMounted(async () => {
  await loadData()
  
  // Poll config values every 3 seconds to sync with VR UI changes
  // Only poll when connected
  pollInterval = setInterval(async () => {
    if (isConnected.value) {
      await configStore.loadConfig()
    }
  }, 3000) as unknown as number
})

onUnmounted(() => {
  if (pollInterval !== null) {
    clearInterval(pollInterval)
  }
})

// Watch connection state and reload when reconnected
watch(isConnected, async (connected, wasConnected) => {
  if (connected && !wasConnected) {
    console.log('[ConfigForm] Connection restored, reloading data')
    await loadData()
  }
})

async function loadData() {
  await configStore.loadSchema()
  await configStore.loadConfig()
}

async function handleUpdate(path: string, value: any) {
  await configStore.updateValue(path, value)
}

async function handleRestart() {
  if (!confirm('Are you sure you want to restart the QuestNav application? This will close the app and reopen it.')) {
    return
  }
  
  try {
    await configApi.restartApp()
    configStore.clearRestartFlag()
    // Show a message that restart is in progress
    alert('Restart initiated. The app will close and reopen shortly.')
  } catch (error) {
    alert('Failed to restart application: ' + (error instanceof Error ? error.message : 'Unknown error'))
  }
}
</script>

<style scoped>
.config-form {
  width: 100%;
  max-width: 1400px;
  margin: 0 auto;
}

/* Restart Banner Styles */
.restart-banner {
  position: sticky;
  top: 0;
  z-index: 100;
  margin-bottom: 1.5rem;
  background: rgba(255, 193, 7, 0.1);
  border: 2px solid var(--warning-color);
  border-left: 4px solid var(--warning-color);
  border-radius: 6px;
  animation: slideDown 0.4s ease;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.4s ease;
}

.slide-down-enter-from {
  opacity: 0;
  transform: translateY(-20px);
}

.slide-down-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}

.restart-content {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.875rem 1rem;
}

.restart-icon {
  font-size: 1.5rem;
  flex-shrink: 0;
}

.restart-message {
  flex: 1;
  color: var(--text-primary);
}

.restart-message strong {
  display: block;
  font-size: 1rem; /* Base text: 16px */
  font-weight: 700;
  margin-bottom: 0.25rem;
}

.restart-message p {
  margin: 0;
  font-size: 0.875rem; /* Small text: 14px */
  font-weight: 600;
  color: var(--text-secondary);
}

.restart-button {
  padding: 0.6rem 1.5rem;
  background: var(--warning-color);
  color: #000;
  border: none;
  border-radius: 6px;
  font-weight: 700;
  font-size: 0.875rem; /* Small text: 14px */
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
  flex-shrink: 0;
}

.restart-button:hover {
  background: var(--amber-dark);
}

.restart-button:active {
  transform: translateY(0);
}


.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1.5rem;
}

.loading-container p {
  color: var(--primary-color);
  font-size: 1rem; /* Base body text: 16px */
  font-weight: 500;
}

.error-container,
.empty-container {
  padding: 3rem 2rem;
  text-align: center;
}

.error-container h3 {
  color: var(--danger-color);
  font-size: 1.5rem; /* Consistent h2 size: 24px */
  margin-bottom: 1rem;
}

.config-content {
  display: flex;
  flex-direction: column;
}

.tabs-container {
  padding: 0;
  overflow: hidden;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
  background: var(--card-bg);
  border: 1px solid var(--border-color);
}

.tabs-nav {
  display: flex;
  gap: 0;
  background: var(--bg-tertiary);
  border-bottom: 2px solid var(--border-color);
  overflow-x: auto;
  scrollbar-width: thin;
  scrollbar-color: var(--primary-color) var(--bg-tertiary);
}

.tabs-nav::-webkit-scrollbar {
  height: 4px;
}

.tabs-nav::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
}

.tabs-nav::-webkit-scrollbar-thumb {
  background: var(--primary-color);
  border-radius: 2px;
}

.tab-button {
  flex: 1;
  min-width: 120px;
  padding: 1rem 1.5rem;
  background-color: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 1rem; /* Base text: 16px (increased from 14px) */
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.6rem;
  position: relative;
  white-space: nowrap;
}

.tab-button::before {
  content: '';
  position: absolute;
  bottom: -3px;
  left: 0;
  right: 0;
  height: 3px;
  background: var(--primary-color);
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.tab-button:hover {
  background: var(--card-bg);
  color: var(--primary-color);
}

.tab-button.active {
  color: var(--text-primary);
  background: var(--card-bg);
}

.tab-button.active::before {
  transform: scaleX(1);
}

.tab-count {
  font-size: 0.75rem;
  padding: 0.2rem 0.6rem;
  background: var(--text-secondary);
  border-radius: 12px;
  color: white;
  font-weight: 700;
  min-width: 24px;
  text-align: center;
}

.tab-button.active .tab-count {
  background: var(--primary-color);
  color: white;
}

.tab-content {
  padding: 2rem;
  min-height: 500px;
  background: var(--card-bg);
}

.tab-panel {
  animation: fadeInSlide 0.3s ease;
}

@keyframes fadeInSlide {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.fields-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.camera-stream {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.5rem;
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, rgba(0, 0, 0, 0.2) 100%);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

@media (max-width: 1024px) {
  .fields-grid {
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  }
}

@media (max-width: 768px) {
  .tabs-nav {
    flex-direction: column;
    border-bottom: none;
    border-right: 2px solid var(--border-color);
  }
  
  .tab-button {
    border-bottom: none;
    border-left: 3px solid transparent;
    justify-content: flex-start;
    padding-left: 2rem;
  }
  
  .tab-button::before {
    left: -2px;
    right: auto;
    width: 3px;
    height: 100%;
    bottom: 0;
    transform: scaleY(0);
  }
  
  .tab-button.active::before {
    transform: scaleY(1);
  }
  
  .tab-content {
    padding: 1.5rem;
  }
  
  .fields-grid {
    grid-template-columns: 1fr;
  }
}
</style>

