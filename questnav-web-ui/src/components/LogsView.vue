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

    <div v-else class="logs-container terminal" ref="logsContainer">
      <div v-if="filteredLogs.length === 0" class="empty-logs">
        No logs to display
      </div>
      
      <div
        v-for="(log, index) in filteredLogs"
        :key="index"
        :class="['log-line', `log-${log.type.toLowerCase()}`]"
      >
        <span class="log-timestamp">{{ formatTime(log.timestamp) }}</span>
        <span :class="['log-level', `level-${log.type.toLowerCase()}`]">{{ getLogPrefix(log.type) }}</span>
        <span class="log-text">{{ log.message }}</span>
        <pre v-if="log.stackTrace && log.type !== 'Log'" class="log-stacktrace">{{ log.stackTrace }}</pre>
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

function handleScroll() {
  if (!logsContainer.value) return
  
  // Check if user has scrolled up from the bottom
  const { scrollTop, scrollHeight, clientHeight } = logsContainer.value
  const isAtBottom = scrollHeight - scrollTop - clientHeight < 10 // 10px threshold
  
  // If user scrolled up and auto-scroll is on, disable it
  if (!isAtBottom && autoScroll.value) {
    autoScroll.value = false
  }
}

function formatTime(timestamp: number): string {
  const date = new Date(timestamp)
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  const seconds = String(date.getSeconds()).padStart(2, '0')
  const ms = String(date.getMilliseconds()).padStart(3, '0')
  return `${hours}:${minutes}:${seconds}.${ms}`
}

function getLogPrefix(type: string): string {
  switch (type) {
    case 'Error': return '[ERROR]'
    case 'Warning': return '[WARN ]'
    case 'Assert': return '[ASSRT]'
    case 'Exception': return '[EXCPT]'
    default: return '[INFO ]'
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
  
  // Attach scroll listener to detect manual scrolling
  if (logsContainer.value) {
    logsContainer.value.addEventListener('scroll', handleScroll)
  }
  
  // Poll logs every 2 seconds
  intervalId = setInterval(loadLogs, 2000) as unknown as number
})

onUnmounted(() => {
  // Remove scroll listener
  if (logsContainer.value) {
    logsContainer.value.removeEventListener('scroll', handleScroll)
  }
  
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
  background: #1e1e1e;
  color: #d4d4d4;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  border: 1px solid #333;
  scrollbar-width: thin;
  scrollbar-color: #555 #2a2a2a;
  line-height: 1.6;
}

.logs-container.terminal {
  background: #1e1e1e;
}

.logs-container::-webkit-scrollbar {
  width: 10px;
}

.logs-container::-webkit-scrollbar-track {
  background: #2a2a2a;
  border-radius: 5px;
}

.logs-container::-webkit-scrollbar-thumb {
  background: #555;
  border-radius: 5px;
}

.logs-container::-webkit-scrollbar-thumb:hover {
  background: #666;
}

.empty-logs {
  text-align: center;
  padding: 3rem;
  color: #666;
  font-size: 1.1rem;
  font-style: italic;
}

.log-line {
  display: flex;
  flex-wrap: wrap;
  padding: 0.25rem 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
}

.log-line:hover {
  background: rgba(255, 255, 255, 0.03);
}

.log-timestamp {
  color: #6a9955;
  margin-right: 0.75rem;
  font-weight: 600;
  flex-shrink: 0;
  user-select: none;
}

.log-level {
  margin-right: 0.75rem;
  font-weight: 700;
  flex-shrink: 0;
  user-select: none;
}

.level-log {
  color: #4ec9b0;
}

.level-warning {
  color: #dcdcaa;
}

.level-error,
.level-exception,
.level-assert {
  color: #f48771;
}

.log-text {
  color: #d4d4d4;
  flex: 1;
  word-break: break-word;
}

.log-stacktrace {
  width: 100%;
  margin-top: 0.5rem;
  padding: 0.75rem;
  background: rgba(0, 0, 0, 0.3);
  border-left: 3px solid #f48771;
  color: #ce9178;
  font-size: 0.8rem;
  line-height: 1.4;
  overflow-x: auto;
  border-radius: 4px;
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

