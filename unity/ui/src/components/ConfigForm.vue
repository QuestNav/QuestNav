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
  max-width: 1200px;
  margin: 0 auto;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem 2rem;
  gap: 1rem;
}

.error-container,
.empty-container {
  padding: 2rem;
  text-align: center;
}

.config-content {
  display: flex;
  flex-direction: column;
}

.tabs-container {
  padding: 0;
  overflow: hidden;
}

.tabs-nav {
  display: flex;
  gap: 0;
  background-color: var(--bg-tertiary);
  border-bottom: 2px solid var(--border-color);
}

.tab-button {
  flex: 1;
  padding: 1rem 1.5rem;
  background-color: transparent;
  border: none;
  border-bottom: 3px solid transparent;
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 0.95rem;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  position: relative;
}

.tab-button:hover {
  background-color: var(--bg-secondary);
  color: var(--text-primary);
}

.tab-button.active {
  background-color: var(--bg-secondary);
  color: var(--primary-color);
  border-bottom-color: var(--primary-color);
}

.tab-count {
  font-size: 0.75rem;
  padding: 0.125rem 0.5rem;
  background-color: var(--bg-tertiary);
  border-radius: 12px;
  color: var(--text-muted);
  font-weight: 500;
}

.tab-button.active .tab-count {
  background-color: var(--primary-color);
  color: #000;
}

.tab-content {
  padding: 2rem;
  min-height: 400px;
}

.tab-panel {
  animation: fadeIn 0.2s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.fields-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .tabs-nav {
    flex-direction: column;
  }
  
  .tab-button {
    border-bottom: none;
    border-left: 3px solid transparent;
  }
  
  .tab-button.active {
    border-left-color: var(--primary-color);
    border-bottom-color: transparent;
  }
  
  .tab-content {
    padding: 1.5rem;
  }
  
  .fields-grid {
    grid-template-columns: 1fr;
  }
}
</style>

