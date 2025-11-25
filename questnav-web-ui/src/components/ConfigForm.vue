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
            <span v-if="tab !== 'Status' && tab !== 'Logs'" class="tab-count">
              {{ configStore.fieldsByCategory[tab]?.length || 0 }}
            </span>
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
import ConfigField from './ConfigField.vue'
import StatusView from './StatusView.vue'
import LogsView from './LogsView.vue'

const configStore = useConfigStore()
const activeTab = ref<string>('Status')
let pollInterval: number | null = null

// Add Status and Logs as first tabs
const allTabs = computed(() => ['Status', 'Logs', ...configStore.categories])

onMounted(async () => {
  await loadData()
  
  // Poll config values every 3 seconds to sync with VR UI changes
  pollInterval = setInterval(async () => {
    await configStore.loadConfig()
  }, 3000) as unknown as number
})

onUnmounted(() => {
  if (pollInterval !== null) {
    clearInterval(pollInterval)
  }
})

async function loadData() {
  await configStore.loadSchema()
  await configStore.loadConfig()
}

async function handleUpdate(path: string, value: any) {
  await configStore.updateValue(path, value)
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
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1.5rem;
}

.loading-container p {
  color: var(--primary-color);
  font-size: 1.1rem;
  font-weight: 500;
}

.error-container,
.empty-container {
  padding: 3rem 2rem;
  text-align: center;
}

.error-container h3 {
  color: var(--danger-color);
  font-size: 1.5rem;
  margin-bottom: 1rem;
}

.config-content {
  display: flex;
  flex-direction: column;
}

.tabs-container {
  padding: 0;
  overflow: hidden;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  background: linear-gradient(135deg, var(--bg-secondary) 0%, var(--bg-tertiary) 100%);
}

.tabs-nav {
  display: flex;
  gap: 0;
  background: linear-gradient(to right, rgba(51, 161, 253, 0.05), rgba(0, 188, 212, 0.05));
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
  padding: 1.2rem 1.5rem;
  background-color: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--text-secondary);
  font-weight: 700;
  font-size: 0.95rem;
  cursor: pointer;
  transition: all 0.3s ease;
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
  bottom: -2px;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, var(--primary-color), var(--teal));
  transform: scaleX(0);
  transition: transform 0.3s ease;
}

.tab-button:hover {
  background: linear-gradient(180deg, rgba(51, 161, 253, 0.1), transparent);
  color: var(--primary-light);
}

.tab-button.active {
  color: var(--primary-color);
  background: linear-gradient(180deg, rgba(51, 161, 253, 0.15), transparent);
}

.tab-button.active::before {
  transform: scaleX(1);
}

.tab-count {
  font-size: 0.75rem;
  padding: 0.2rem 0.6rem;
  background: var(--primary-color);
  border-radius: 12px;
  color: white;
  font-weight: 700;
  min-width: 24px;
  text-align: center;
}

.tab-button.active .tab-count {
  background: white;
  color: var(--primary-color);
  box-shadow: 0 2px 8px rgba(51, 161, 253, 0.4);
}

.tab-content {
  padding: 2.5rem;
  min-height: 500px;
  background: var(--bg-secondary);
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

