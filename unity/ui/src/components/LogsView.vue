<template>
  <div class="logs-view">
    <div class="logs-controls">
      <div class="controls-left">
        <button @click="loadLogs" class="secondary">🔄 Refresh</button>
        <button @click="exportLogs" class="secondary">💾 Export</button>
        <button @click="clearLogs" class="danger">🗑️ Clear Logs</button>
        <label class="auto-scroll-label">
          <input type="checkbox" v-model="autoScroll" />
          Auto-scroll
        </label>
      </div>
      <div class="controls-right">
        <select v-model="filterLevel" class="filter-select">
          <option value="all">All Levels</option>
          <option value="Log">Log</option>
          <option value="Warning">Warning</option>
          <option value="Error">Error</option>
        </select>
        <span class="log-count">{{ filteredLogs.length }} logs</span>
      </div>
    </div>

    <div v-if="loading" class="loading-container">
      <div class="spinner"></div>
      <p>Loading logs...</p>
    </div>

    <div v-else-if="error" class="error-message">
      {{ error }}
    </div>

    <div v-else class="logs-container card" ref="logsContainer">
      <div v-if="filteredLogs.length === 0" class="empty-logs">
        No logs to display
      </div>
      
      <div
        v-for="(log, index) in filteredLogs"
        :key="index"
        :class="['log-entry', `log-${log.type.toLowerCase()}`]"
      >
        <div class="log-header">
          <span :class="['log-type', `type-${log.type.toLowerCase()}`]">
            {{ getLogIcon(log.type) }} {{ log.type }}
          </span>
          <span class="log-time">{{ formatTime(log.timestamp) }}</span>
        </div>
        <div class="log-message">{{ log.message }}</div>
        <div v-if="log.stackTrace && log.type !== 'Log'" class="log-stack">
          <details>
            <summary>Stack Trace</summary>
            <pre>{{ log.stackTrace }}</pre>
          </details>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { configApi } from '../api/config'

interface LogEntry {
  message: string
  stackTrace: string
  type: string
  timestamp: number
}

const logs = ref<LogEntry[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const filterLevel = ref('all')
const autoScroll = ref(true)
const logsContainer = ref<HTMLElement | null>(null)
let intervalId: number | null = null

const filteredLogs = computed(() => {
  if (filterLevel.value === 'all') return logs.value
  return logs.value.filter(log => log.type === filterLevel.value)
})

async function loadLogs() {
  try {
    error.value = null
    const response = await configApi.getLogs(200)
    logs.value = response.logs
    loading.value = false
    
    if (autoScroll.value) {
      await nextTick()
      scrollToBottom()
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load logs'
    loading.value = false
  }
}

async function clearLogs() {
  if (!confirm('Are you sure you want to clear all logs?')) return
  
  try {
    await configApi.clearLogs()
    logs.value = []
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to clear logs'
  }
}

function exportLogs() {
  // Generate log file content
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-').slice(0, -5)
  const filename = `questnav-logs-${timestamp}.log`
  
  let content = `QuestNav Unity Logs\n`
  content += `Exported: ${new Date().toLocaleString()}\n`
  content += `Total Logs: ${logs.value.length}\n`
  content += `Filter: ${filterLevel.value}\n`
  content += `${'='.repeat(80)}\n\n`
  
  filteredLogs.value.forEach((log, index) => {
    const time = new Date(log.timestamp).toLocaleString()
    content += `[${index + 1}] ${time} [${log.type}]\n`
    content += `${log.message}\n`
    
    if (log.stackTrace && log.type !== 'Log') {
      content += `\nStack Trace:\n${log.stackTrace}\n`
    }
    
    content += `${'-'.repeat(80)}\n\n`
  })
  
  // Create and download file
  const blob = new Blob([content], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}

function scrollToBottom() {
  if (logsContainer.value) {
    logsContainer.value.scrollTop = logsContainer.value.scrollHeight
  }
}

function formatTime(timestamp: number): string {
  const date = new Date(timestamp)
  return date.toLocaleTimeString() + '.' + String(date.getMilliseconds()).padStart(3, '0')
}

function getLogIcon(type: string): string {
  switch (type) {
    case 'Error': return '❌'
    case 'Warning': return '⚠️'
    case 'Assert': return '🛑'
    case 'Exception': return '💥'
    default: return '📝'
  }
}

watch(filteredLogs, async () => {
  if (autoScroll.value) {
    await nextTick()
    scrollToBottom()
  }
})

onMounted(async () => {
  await loadLogs()
  // Poll logs every 2 seconds
  intervalId = setInterval(loadLogs, 2000) as unknown as number
})

onUnmounted(() => {
  if (intervalId !== null) {
    clearInterval(intervalId)
  }
})
</script>

<style scoped>
.logs-view {
  width: 100%;
  max-width: 1400px;
  margin: 0 auto;
}

.logs-controls {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  padding: 1rem;
  background: white;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.controls-left,
.controls-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.auto-scroll-label {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: var(--text-primary);
  cursor: pointer;
  font-size: 0.95rem;
  font-weight: 600;
  padding: 0.5rem 1rem;
  background: var(--bg-tertiary);
  border-radius: 8px;
  transition: all 0.2s ease;
  border: 1px solid var(--border-color);
}

.auto-scroll-label:hover {
  background: var(--border-color);
  color: var(--primary-color);
}

.auto-scroll-label input[type="checkbox"] {
  width: 1.2rem;
  height: 1.2rem;
  cursor: pointer;
  accent-color: var(--primary-color);
}

.filter-select {
  padding: 0.6rem 1rem;
  background: white;
  border: 2px solid var(--border-color);
  border-radius: 8px;
  color: var(--text-primary);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.filter-select:hover {
  border-color: var(--primary-color);
  background: white;
}

.filter-select:focus {
  box-shadow: 0 0 12px rgba(51, 161, 253, 0.3);
}

.log-count {
  font-size: 0.9rem;
  color: var(--primary-color);
  font-weight: 700;
  background: rgba(51, 161, 253, 0.15);
  padding: 0.5rem 1rem;
  border-radius: 8px;
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

.error-message {
  padding: 2rem;
  background: linear-gradient(135deg, rgba(220, 53, 69, 0.15), rgba(220, 53, 69, 0.05));
  border: 2px solid var(--danger-color);
  border-radius: 12px;
  color: var(--danger-color);
  text-align: center;
  font-weight: 600;
  box-shadow: 0 4px 20px rgba(220, 53, 69, 0.2);
}

.logs-container {
  max-height: 600px;
  overflow-y: auto;
  padding: 1.5rem;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.875rem;
  background: white;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  border: 1px solid var(--border-color);
  scrollbar-width: thin;
  scrollbar-color: var(--primary-color) var(--bg-tertiary);
}

.logs-container::-webkit-scrollbar {
  width: 10px;
}

.logs-container::-webkit-scrollbar-track {
  background: var(--bg-tertiary);
  border-radius: 5px;
}

.logs-container::-webkit-scrollbar-thumb {
  background: var(--primary-color);
  border-radius: 5px;
}

.logs-container::-webkit-scrollbar-thumb:hover {
  background: var(--primary-light);
}

.empty-logs {
  text-align: center;
  padding: 3rem;
  color: var(--text-muted);
  font-size: 1.1rem;
  font-style: italic;
}

.log-entry {
  margin-bottom: 1rem;
  padding: 1rem;
  border-left: 4px solid var(--border-color);
  background: var(--bg-tertiary);
  border-radius: 8px;
  transition: all 0.2s ease;
  animation: slideIn 0.2s ease;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-10px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.log-entry:hover {
  background: var(--border-color);
  transform: translateX(5px);
}

.log-entry.log-log {
  border-left-color: var(--text-secondary);
}

.log-entry.log-warning {
  border-left-color: var(--warning-color);
  background: rgba(255, 193, 7, 0.1);
}

.log-entry.log-error,
.log-entry.log-exception,
.log-entry.log-assert {
  border-left-color: var(--danger-color);
  background: rgba(220, 53, 69, 0.1);
}

.log-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.75rem;
  gap: 1rem;
  flex-wrap: wrap;
}

.log-type {
  font-weight: 700;
  font-size: 0.85rem;
  padding: 0.4rem 0.75rem;
  border-radius: 6px;
  background: var(--bg-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border: 1px solid var(--border-color);
}

.type-log {
  color: var(--text-secondary);
}

.type-warning {
  color: var(--warning-color);
  background: rgba(255, 193, 7, 0.15);
}

.type-error,
.type-exception,
.type-assert {
  color: var(--danger-color);
  background: rgba(220, 53, 69, 0.15);
}

.log-time {
  font-size: 0.8rem;
  color: var(--text-muted);
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  background: var(--bg-tertiary);
  padding: 0.3rem 0.6rem;
  border-radius: 4px;
  border: 1px solid var(--border-color);
}

.log-message {
  color: var(--text-primary);
  line-height: 1.6;
  word-break: break-word;
  font-size: 0.9rem;
}

.log-stack {
  margin-top: 0.75rem;
}

.log-stack details {
  cursor: pointer;
}

.log-stack summary {
  color: var(--primary-color);
  font-size: 0.85rem;
  font-weight: 600;
  padding: 0.5rem;
  background: rgba(51, 161, 253, 0.1);
  border-radius: 6px;
  user-select: none;
  transition: all 0.2s ease;
  border: 1px solid rgba(51, 161, 253, 0.2);
}

.log-stack summary:hover {
  color: var(--primary-dark);
  background: rgba(51, 161, 253, 0.15);
}

.log-stack pre {
  margin-top: 0.75rem;
  padding: 1rem;
  background-color: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  overflow-x: auto;
  font-size: 0.8rem;
  color: var(--text-secondary);
  line-height: 1.5;
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.05);
}

@media (max-width: 768px) {
  .logs-controls {
    flex-direction: column;
    align-items: stretch;
  }
  
  .controls-left,
  .controls-right {
    justify-content: space-between;
  }
  
  .logs-container {
    max-height: 500px;
    padding: 1rem;
  }
}
</style>

