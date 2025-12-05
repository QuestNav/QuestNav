<template>
  <div id="app">
    <!-- Disconnected Overlay -->
    <transition name="fade">
      <div v-if="isDisconnected" class="disconnect-overlay">
        <div class="disconnect-card card">
          <div class="disconnect-icon">📡</div>
          <h2>Connection Lost</h2>
          <p>Lost connection to QuestNav</p>
          <div class="reconnect-info">
            <div v-if="secondsUntilRetry > 0" class="retry-countdown">
              <span class="spinner-small"></span>
              Retrying in {{ secondsUntilRetry }} second{{ secondsUntilRetry !== 1 ? 's' : '' }}...
            </div>
            <div v-else class="retry-countdown">
              <span class="spinner-small"></span>
              Reconnecting...
            </div>
          </div>
          <button @click="manualRetry" class="retry-button">
            Retry Now
          </button>
        </div>
      </div>
    </transition>

    <!-- Main Application -->
    <div class="app-container">
      <!-- Header -->
      <header class="app-header">
        <div class="header-content">
          <div class="header-left">
            <div class="logo-container">
              <img :src="isDarkMode ? '/logo-dark.svg' : '/logo.svg'" alt="QuestNav" class="logo" />
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
              <span class="button-text">Restart App</span>
            </button>
            <button class="icon-button theme-toggle" @click="toggleTheme" :title="isDarkMode ? 'Switch to Light Mode' : 'Switch to Dark Mode'">
              <svg v-if="!isDarkMode" class="theme-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="5"/>
                <line x1="12" y1="1" x2="12" y2="3"/>
                <line x1="12" y1="21" x2="12" y2="23"/>
                <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/>
                <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
                <line x1="1" y1="12" x2="3" y2="12"/>
                <line x1="21" y1="12" x2="23" y2="12"/>
                <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/>
                <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
              </svg>
              <svg v-else class="theme-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
              </svg>
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
            <span class="info-label">Operating System:</span>
            <span class="info-value">{{ serverInfo.operatingSystem }}</span>
          </div>
          
          <h3>Server</h3>
          <div class="info-item">
            <span class="info-label">Port:</span>
            <span class="info-value">{{ serverInfo.serverPort }}</span>
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
import { useConnectionState } from './composables/useConnectionState'
import { configApi } from './api/config'
import ConfigForm from './components/ConfigForm.vue'
import type { ServerInfo } from './types'

const configStore = useConfigStore()
const { 
  connectionStatus, 
  isConnected, 
  isDisconnected, 
  secondsUntilRetry,
  checkConnection, 
  getReconnectDelay 
} = useConnectionState()

const showInfoModal = ref(false)
const serverInfo = ref<ServerInfo | null>(null)
const isDarkMode = ref(false)

// Initialize theme from localStorage or system preference
onMounted(async () => {
  // Check localStorage first
  const savedTheme = localStorage.getItem('questnav-theme')
  if (savedTheme) {
    isDarkMode.value = savedTheme === 'dark'
  } else {
    // Fall back to system preference
    isDarkMode.value = window.matchMedia('(prefers-color-scheme: dark)').matches
  }
  applyTheme()
  
  // Initial check
  await scheduleNextCheck()
})

function applyTheme() {
  if (isDarkMode.value) {
    document.documentElement.setAttribute('data-theme', 'dark')
  } else {
    document.documentElement.removeAttribute('data-theme')
  }
}

function toggleTheme() {
  isDarkMode.value = !isDarkMode.value
  localStorage.setItem('questnav-theme', isDarkMode.value ? 'dark' : 'light')
  applyTheme()
}

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

// Monitor connection status with exponential backoff
let statusCheckInterval: number | null = null

async function scheduleNextCheck() {
  const connected = await checkConnection(false) // Automatic retry
  
  // Schedule next check based on connection state
  const delay = connected ? 3000 : getReconnectDelay()
  
  if (statusCheckInterval !== null) {
    clearTimeout(statusCheckInterval)
  }
  
  statusCheckInterval = setTimeout(scheduleNextCheck, delay) as unknown as number
}

async function manualRetry() {
  // Cancel the scheduled automatic retry
  if (statusCheckInterval !== null) {
    clearTimeout(statusCheckInterval)
  }
  
  // Try to connect immediately with manual flag (doesn't increment backoff)
  const connected = await checkConnection(true) // Manual retry
  
  // Schedule next automatic check based on result
  const delay = connected ? 3000 : getReconnectDelay()
  statusCheckInterval = setTimeout(scheduleNextCheck, delay) as unknown as number
}

onMounted(async () => {
  // Initial check
  await scheduleNextCheck()
})

onUnmounted(() => {
  if (statusCheckInterval !== null) {
    clearTimeout(statusCheckInterval)
  }
})

async function refreshData() {
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
/* Disconnect Overlay */
.disconnect-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: transparent;
  backdrop-filter: blur(10px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
  padding: 2rem;
}

.disconnect-card {
  max-width: 500px;
  padding: 3rem;
  text-align: center;
  background: rgba(220, 53, 69, 0.65);
  border: 2px solid var(--danger-color);
  box-shadow: 0 20px 60px rgba(220, 53, 69, 0.4);
  backdrop-filter: blur(20px);
}

.disconnect-icon {
  font-size: 4rem;
  margin-bottom: 1.5rem;
  filter: grayscale(100%) brightness(0.8);
  animation: disconnectPulse 2s ease-in-out infinite;
}

@keyframes disconnectPulse {
  0%, 100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(1.1);
  }
}

.disconnect-card h2 {
  color: #fff;
  font-size: 1.5rem; /* Consistent h2 size: 24px */
  margin-bottom: 1rem;
  font-weight: 700;
  text-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}

.disconnect-card > p {
  color: rgba(255, 255, 255, 0.95);
  font-size: 1rem; /* Base body text: 16px */
  margin-bottom: 2rem;
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
}

.reconnect-info {
  margin: 2rem 0;
  padding: 1.5rem;
  background: rgba(0, 0, 0, 0.2);
  border-radius: 12px;
  border: 1px solid rgba(220, 53, 69, 0.3);
}

.retry-countdown {
  font-size: 1rem; /* Base body text: 16px */
  color: #fff;
  font-weight: 600;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
}

.spinner-small {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.9);
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.retry-button {
  padding: 1rem 2.5rem;
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 2px solid rgba(255, 255, 255, 0.4);
  border-radius: 12px;
  font-weight: 700;
  font-size: 1rem; /* Base button text: 16px */
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
  margin-top: 1rem;
  backdrop-filter: blur(10px);
  text-shadow: 0 1px 4px rgba(0, 0, 0, 0.3);
}

.retry-button:hover {
  background: rgba(255, 255, 255, 0.3);
  border-color: rgba(255, 255, 255, 0.6);
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.3);
}

.retry-button:active {
  transform: translateY(0);
}

.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from, .fade-leave-to {
  opacity: 0;
}

.app-container {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-color);
}

.app-header {
  background: var(--header-bg);
  border-bottom: 1px solid var(--border-color);
  padding: 1rem 2rem;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
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
  filter: drop-shadow(0 2px 6px rgba(51, 161, 253, 0.35));
}

.header-info {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.last-updated {
  font-size: 0.875rem; /* Small text: 14px */
  color: var(--text-secondary);
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.info-icon {
  font-size: 0.875rem; /* Small text: 14px */
}

.connection-status {
  font-size: 0.875rem; /* Small text: 14px */
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
  background: rgba(76, 175, 80, 0.25);
  border: 2px solid var(--success-color);
  box-shadow: 0 2px 4px rgba(76, 175, 80, 0.3);
}

.connection-status.connected .status-dot {
  background-color: var(--success-color);
  box-shadow: 0 0 8px var(--success-color);
}

.connection-status.disconnected {
  color: var(--danger-color);
  background: rgba(220, 53, 69, 0.15);
  border: 2px solid var(--danger-color);
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.3);
}

.connection-status.disconnected .status-dot {
  background-color: var(--danger-color);
}

.connection-status.connecting {
  color: var(--warning-color);
  background: rgba(255, 193, 7, 0.15);
  border: 2px solid var(--amber-dark);
  box-shadow: 0 2px 4px rgba(255, 193, 7, 0.3);
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
  font-size: 0.875rem; /* Small text: 14px */
}

.icon-button.theme-toggle {
  padding: 0.6rem;
  min-width: auto;
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-primary);
}

.icon-button.theme-toggle:hover {
  background: var(--bg-tertiary);
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.theme-icon {
  width: 20px;
  height: 20px;
  stroke: currentColor;
}

.button-icon {
  font-size: 1rem; /* Base icon size: 16px */
}

.app-content {
  flex: 1;
  padding: 2rem;
  max-width: 1400px;
  width: 100%;
  margin: 0 auto;
}

.app-footer {
  background: var(--header-bg);
  border-top: 1px solid var(--border-color);
  padding: 1.5rem 2rem;
  margin-top: auto;
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
  font-size: 0.875rem; /* Small text: 14px */
}

.footer-links {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.footer-links a {
  color: var(--text-primary);
  text-decoration: none;
  font-weight: 600;
  transition: all 0.2s ease;
  font-size: 0.875rem; /* Small text: 14px */
}

.footer-links a:hover {
  color: var(--primary-color);
  text-decoration: underline;
}

.separator {
  color: var(--border-color);
  font-size: 0.75rem; /* Extra small: 12px */
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
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
  color: var(--text-primary);
  margin-bottom: 1rem;
  font-size: 1.5rem; /* Consistent h2 size: 24px */
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
  color: var(--text-primary);
  font-size: 1.125rem; /* Consistent h3 size: 18px */
  border-bottom: 2px solid var(--border-color);
  padding-bottom: 0.5rem;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.info-grid h3::before {
  content: '▶';
  font-size: 0.75rem; /* Extra small: 12px */
}

.info-grid h3:first-of-type {
  margin-top: 0;
}

.info-item {
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: 1rem;
  padding: 0.75rem;
  background: var(--bg-tertiary);
  border-radius: 6px;
  border-left: 3px solid var(--primary-color);
  transition: all 0.2s ease;
}

.info-item:hover {
  background: var(--bg-secondary);
}

.info-label {
  font-weight: 600;
  color: var(--text-secondary);
  font-size: 0.875rem; /* Small text: 14px */
}

.info-value {
  color: var(--text-primary);
  word-break: break-all;
  font-size: 0.875rem; /* Small text: 14px */
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
  
  .icon-button.theme-toggle {
    flex: 0;
    min-width: 44px;
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

