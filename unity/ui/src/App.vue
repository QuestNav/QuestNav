<template>
  <div id="app">
    <!-- Main Application -->
    <div class="app-container">
      <!-- Header -->
      <header class="app-header">
        <div class="header-content">
          <div class="header-left">
            <div class="logo-container">
              <img src="/logo.svg" alt="QuestNav" class="logo" />
            </div>
            <div class="header-info">
              <span v-if="configStore.lastUpdated" class="last-updated">
                <span class="info-icon">🕐</span> Updated {{ formatTime(configStore.lastUpdated) }}
              </span>
              <span :class="['connection-status', connectionStatus]">
                <span class="status-dot"></span>
                {{ connectionStatusText }}
              </span>
            </div>
          </div>
          
          <div class="header-right">
            <button class="secondary icon-button" @click="refreshData" title="Refresh">
              <span class="button-icon">🔄</span>
              <span class="button-text">Refresh</span>
            </button>
            <button class="secondary icon-button" @click="showInfo" title="Server Info">
              <span class="button-icon">ℹ️</span>
              <span class="button-text">Info</span>
            </button>
            <button class="danger icon-button" @click="handleRestart" title="Restart App">
              <span class="button-icon">⚡</span>
              <span class="button-text">Restart</span>
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
        <div class="footer-content">
          <p class="footer-text">
            QuestNav Configuration Interface • Built for Meta Quest
          </p>
          <div class="footer-links">
            <a href="https://questnav.gg" target="_blank" rel="noopener noreferrer">Documentation</a>
            <span class="separator">•</span>
            <a href="https://github.com/QuestNav/QuestNav" target="_blank" rel="noopener noreferrer">GitHub</a>
            <span class="separator">•</span>
            <a href="https://discord.gg/hD3FtR7YAZ" target="_blank" rel="noopener noreferrer">Discord</a>
          </div>
        </div>
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
  background: linear-gradient(to bottom, var(--bg-color) 0%, #1a1f21 100%);
}

.app-header {
  background: linear-gradient(135deg, var(--bg-secondary) 0%, var(--bg-tertiary) 100%);
  border-bottom: 2px solid var(--primary-color);
  padding: 1.25rem 2rem;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(10px);
}

.header-content {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1.5rem;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 2rem;
}

.logo-container {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.logo {
  height: 48px;
  width: auto;
  filter: drop-shadow(0 2px 4px rgba(51, 161, 253, 0.2));
}

.header-info {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.last-updated {
  font-size: 0.85rem;
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.info-icon {
  font-size: 0.9rem;
}

.connection-status {
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  padding: 0.25rem 0.75rem;
  border-radius: 12px;
  background: rgba(0, 0, 0, 0.2);
}

.connection-status .status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  display: inline-block;
}

.connection-status.connected {
  color: var(--success-color);
  background: rgba(76, 175, 80, 0.15);
}

.connection-status.connected .status-dot {
  background-color: var(--success-color);
  box-shadow: 0 0 8px var(--success-color);
}

.connection-status.disconnected {
  color: var(--danger-color);
  background: rgba(220, 53, 69, 0.15);
}

.connection-status.disconnected .status-dot {
  background-color: var(--danger-color);
}

.connection-status.connecting {
  color: var(--warning-color);
  background: rgba(255, 193, 7, 0.15);
}

.connection-status.connecting .status-dot {
  background-color: var(--warning-color);
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { 
    opacity: 1;
    transform: scale(1);
  }
  50% { 
    opacity: 0.3;
    transform: scale(0.8);
  }
}

.header-right {
  display: flex;
  gap: 0.75rem;
}

.icon-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1.2rem;
  font-size: 0.95rem;
}

.button-icon {
  font-size: 1.1rem;
}

.app-content {
  flex: 1;
  padding: 2rem;
  max-width: 1400px;
  width: 100%;
  margin: 0 auto;
}

.app-footer {
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, var(--bg-secondary) 100%);
  border-top: 2px solid var(--primary-color);
  padding: 1.5rem 2rem;
  box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.3);
}

.footer-content {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 1rem;
}

.footer-text {
  margin: 0;
  color: var(--text-secondary);
  font-size: 0.9rem;
}

.footer-links {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.footer-links a {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
  font-size: 0.9rem;
}

.footer-links a:hover {
  color: var(--primary-light);
  text-decoration: underline;
}

.separator {
  color: var(--border-color);
  font-size: 0.8rem;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
  backdrop-filter: blur(4px);
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.modal-content {
  max-width: 700px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  animation: slideUp 0.3s ease;
}

@keyframes slideUp {
  from {
    transform: translateY(30px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-content h2 {
  color: var(--primary-color);
  margin-bottom: 1rem;
  font-size: 1.75rem;
}

.info-grid {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-top: 1.5rem;
}

.info-grid h3 {
  margin-top: 1.5rem;
  margin-bottom: 0.75rem;
  color: var(--primary-color);
  font-size: 1.1rem;
  border-bottom: 2px solid var(--primary-color);
  padding-bottom: 0.5rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.info-grid h3::before {
  content: '▶';
  font-size: 0.8rem;
}

.info-grid h3:first-of-type {
  margin-top: 0;
}

.info-item {
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: 1rem;
  padding: 1rem;
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, rgba(0, 0, 0, 0.2) 100%);
  border-radius: 8px;
  border-left: 3px solid var(--primary-color);
  transition: all 0.2s ease;
}

.info-item:hover {
  background: linear-gradient(135deg, var(--border-color) 0%, rgba(0, 0, 0, 0.3) 100%);
  transform: translateX(5px);
}

.info-label {
  font-weight: 600;
  color: var(--primary-light);
  font-size: 0.9rem;
}

.info-value {
  color: var(--text-primary);
  word-break: break-all;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.9rem;
}

@media (max-width: 1024px) {
  .header-content {
    max-width: 100%;
  }
  
  .app-content {
    max-width: 100%;
  }
  
  .footer-content {
    max-width: 100%;
  }
}

@media (max-width: 768px) {
  .header-content {
    flex-direction: column;
    align-items: stretch;
  }
  
  .header-left {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
  
  .logo-container {
    width: 100%;
  }
  
  .header-right {
    justify-content: space-between;
    flex-wrap: wrap;
  }
  
  .icon-button .button-text {
    display: none;
  }
  
  .icon-button {
    padding: 0.6rem;
    flex: 1;
    justify-content: center;
  }
  
  .app-content {
    padding: 1rem;
  }
  
  .footer-content {
    flex-direction: column;
    text-align: center;
  }
  
  .info-item {
    grid-template-columns: 1fr;
    gap: 0.5rem;
  }
}
</style>

