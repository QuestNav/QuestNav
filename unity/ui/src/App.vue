<template>
  <div id="app">
    <!-- Main Application -->
    <div class="app-container">
      <!-- Header -->
      <header class="app-header">
        <div class="header-content">
          <div class="header-left">
            <h1>🎮 QuestNav Config</h1>
            <div class="header-info">
              <span v-if="configStore.lastUpdated" class="last-updated">
                Last updated: {{ formatTime(configStore.lastUpdated) }}
              </span>
              <span :class="['connection-status', connectionStatus]">
                <span class="status-dot"></span>
                {{ connectionStatusText }}
              </span>
            </div>
          </div>
          
          <div class="header-right">
            <button class="secondary" @click="refreshData">
              🔄 Refresh
            </button>
            <button class="secondary" @click="showInfo">
              ℹ️ Info
            </button>
            <button class="danger" @click="handleRestart">
              🔄 Restart App
            </button>
          </div>
        </div>
      </header>

      <!-- Content -->
      <main class="app-content">
        <ConfigForm />
      </main>

      <!-- Footer -->
      <footer class="app-footer">
        <p class="text-muted">
          QuestNav Configuration UI • 
          <a href="https://github.com/yourusername/questnav" target="_blank">GitHub</a>
        </p>
      </footer>
    </div>

    <!-- Info Modal -->
    <div v-if="showInfoModal" class="modal-overlay" @click="showInfoModal = false">
      <div class="modal-content card" @click.stop>
        <h2>Server Information</h2>
        
        <div v-if="serverInfo" class="info-grid">
          <h3>Application</h3>
          <div class="info-item">
            <span class="info-label">App Name:</span>
            <span class="info-value">{{ serverInfo.appName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Version:</span>
            <span class="info-value">{{ serverInfo.version }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Build Date:</span>
            <span class="info-value">{{ serverInfo.buildDate }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Unity Version:</span>
            <span class="info-value">{{ serverInfo.unityVersion }}</span>
          </div>
          
          <h3>Device</h3>
          <div class="info-item">
            <span class="info-label">Device Model:</span>
            <span class="info-value">{{ serverInfo.deviceModel }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Device Name:</span>
            <span class="info-value">{{ serverInfo.deviceName }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Operating System:</span>
            <span class="info-value">{{ serverInfo.operatingSystem }}</span>
          </div>
          
          <h3>System</h3>
          <div class="info-item">
            <span class="info-label">Processor:</span>
            <span class="info-value">{{ serverInfo.processorType }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">CPU Cores:</span>
            <span class="info-value">{{ serverInfo.processorCount }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">System Memory:</span>
            <span class="info-value">{{ serverInfo.systemMemorySize }} MB</span>
          </div>
          <div class="info-item">
            <span class="info-label">Graphics:</span>
            <span class="info-value">{{ serverInfo.graphicsDeviceName }}</span>
          </div>
          
          <h3>Server</h3>
          <div class="info-item">
            <span class="info-label">Port:</span>
            <span class="info-value">{{ serverInfo.serverPort }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">Config Path:</span>
            <span class="info-value">{{ serverInfo.configPath }}</span>
          </div>
        </div>
        
        <button @click="showInfoModal = false" class="mt-2">Close</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useConfigStore } from './stores/config'
import { configApi } from './api/config'
import ConfigForm from './components/ConfigForm.vue'
import type { ServerInfo } from './types'

const configStore = useConfigStore()
const showInfoModal = ref(false)
const serverInfo = ref<ServerInfo | null>(null)
const connectionStatus = ref<'connected' | 'disconnected' | 'connecting'>('connecting')

const connectionStatusText = computed(() => {
  switch (connectionStatus.value) {
    case 'connected': return 'Connected'
    case 'disconnected': return 'Disconnected'
    case 'connecting': return 'Connecting...'
  }
})

function formatTime(timestamp: number): string {
  const date = new Date(timestamp)
  return date.toLocaleTimeString()
}

// Monitor connection status
let statusCheckInterval: number | null = null

onMounted(() => {
  // Check connection every 3 seconds
  statusCheckInterval = setInterval(async () => {
    try {
      await configApi.getSchema()
      connectionStatus.value = 'connected'
    } catch {
      connectionStatus.value = 'disconnected'
    }
  }, 3000) as unknown as number
  
  // Initial check
  setTimeout(async () => {
    try {
      await configApi.getSchema()
      connectionStatus.value = 'connected'
    } catch {
      connectionStatus.value = 'disconnected'
    }
  }, 500)
})

onUnmounted(() => {
  if (statusCheckInterval !== null) {
    clearInterval(statusCheckInterval)
  }
})

async function refreshData() {
  await configStore.loadSchema()
  await configStore.loadConfig()
}

async function showInfo() {
  try {
    serverInfo.value = await configApi.getServerInfo()
    showInfoModal.value = true
  } catch (error) {
    console.error('Failed to load server info:', error)
  }
}

async function handleRestart() {
  if (!confirm('Are you sure you want to restart the QuestNav application? This will disconnect and restart the app on the Quest headset.')) {
    return
  }
  
  try {
    await configApi.restartApp()
    // Show message that restart was triggered
    alert('Restart command sent. The app will restart shortly.')
  } catch (error) {
    console.error('Failed to restart app:', error)
    alert('Failed to send restart command: ' + (error instanceof Error ? error.message : 'Unknown error'))
  }
}
</script>

<style scoped>
.app-container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  background-color: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  padding: 1rem 2rem;
  position: sticky;
  top: 0;
  z-index: 100;
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 1.5rem;
}

.header-left h1 {
  margin: 0;
  font-size: 1.5rem;
}

.header-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.last-updated {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.connection-status {
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
}

.connection-status .status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
}

.connection-status.connected {
  color: var(--success-color);
}

.connection-status.connected .status-dot {
  background-color: var(--success-color);
}

.connection-status.disconnected {
  color: var(--danger-color);
}

.connection-status.disconnected .status-dot {
  background-color: var(--danger-color);
}

.connection-status.connecting {
  color: var(--warning-color);
}

.connection-status.connecting .status-dot {
  background-color: var(--warning-color);
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}

.header-right {
  display: flex;
  gap: 0.75rem;
}

.app-content {
  flex: 1;
  padding: 2rem;
}

.app-footer {
  background-color: var(--bg-secondary);
  border-top: 1px solid var(--border-color);
  padding: 1.5rem 2rem;
  text-align: center;
}

.app-footer a {
  color: var(--primary-color);
  text-decoration: none;
}

.app-footer a:hover {
  text-decoration: underline;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
}

.modal-content {
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
}

.info-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1.5rem;
}

.info-grid h3 {
  margin-top: 1rem;
  margin-bottom: 0.5rem;
  color: var(--primary-color);
  font-size: 1rem;
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 0.5rem;
}

.info-grid h3:first-child {
  margin-top: 0;
}

.info-item {
  display: grid;
  grid-template-columns: 150px 1fr;
  gap: 1rem;
  padding: 0.75rem;
  background-color: var(--bg-tertiary);
  border-radius: 6px;
}

.info-label {
  font-weight: 600;
  color: var(--text-secondary);
}

.info-value {
  color: var(--text-primary);
  word-break: break-all;
  font-family: monospace;
}

@media (max-width: 768px) {
  .header-content {
    flex-direction: column;
    align-items: stretch;
  }
  
  .header-left {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.75rem;
  }
  
  .header-right {
    justify-content: space-between;
  }
  
  .app-content {
    padding: 1rem;
  }
  
  .info-item {
    grid-template-columns: 1fr;
  }
}
</style>

