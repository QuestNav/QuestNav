<template>
  <div class="status-view">
    <div v-if="loading" class="loading-container">
      <div class="spinner"></div>
      <p>Loading status...</p>
    </div>

    <div v-else-if="error" class="error-message">
      {{ error }}
    </div>

    <div v-else-if="status" class="status-grid">
      <!-- Network Status -->
      <div class="status-card card">
        <h3>🌐 Network</h3>
        <div class="status-items">
          <div class="status-item">
            <span class="label">Connection:</span>
            <span :class="['value', status.networkConnected ? 'success' : 'error']">
              {{ status.networkConnected ? 'Connected' : 'Disconnected' }}
            </span>
          </div>
          <div class="status-item">
            <span class="label">IP Address:</span>
            <span class="value mono">{{ status.ipAddress }}</span>
          </div>
          <div class="status-item">
            <span class="label">Team Number:</span>
            <span class="value">{{ status.teamNumber }}</span>
          </div>
        </div>
      </div>

      <!-- Position & Tracking -->
      <div class="status-card card">
        <h3>📍 Position</h3>
        <div class="status-items">
          <div class="status-item">
            <span class="label">X:</span>
            <span class="value mono">{{ status.position.x.toFixed(3) }} m</span>
          </div>
          <div class="status-item">
            <span class="label">Y:</span>
            <span class="value mono">{{ status.position.y.toFixed(3) }} m</span>
          </div>
          <div class="status-item">
            <span class="label">Z:</span>
            <span class="value mono">{{ status.position.z.toFixed(3) }} m</span>
          </div>
          <div class="status-item">
            <span class="label">Pitch:</span>
            <span class="value mono">{{ status.eulerAngles.pitch.toFixed(1) }}°</span>
          </div>
          <div class="status-item">
            <span class="label">Yaw:</span>
            <span class="value mono">{{ status.eulerAngles.yaw.toFixed(1) }}°</span>
          </div>
          <div class="status-item">
            <span class="label">Roll:</span>
            <span class="value mono">{{ status.eulerAngles.roll.toFixed(1) }}°</span>
          </div>
        </div>
        <button @click="handleResetPose" class="reset-button">
          🎯 Recenter Tracking
        </button>
      </div>

      <!-- Tracking Status -->
      <div class="status-card card">
        <h3>🎯 Tracking</h3>
        <div class="status-items">
          <div class="status-item">
            <span class="label">Status:</span>
            <span :class="['value', status.isTracking ? 'success' : 'error']">
              {{ status.isTracking ? 'Tracking' : 'Lost' }}
            </span>
          </div>
          <div class="status-item">
            <span class="label">Tracking Lost Events:</span>
            <span class="value">{{ status.trackingLostEvents }}</span>
          </div>
        </div>
      </div>

      <!-- Battery -->
      <div class="status-card card">
        <h3>🔋 Battery</h3>
        <div class="status-items">
          <div class="status-item">
            <span class="label">Level:</span>
            <div class="battery-bar">
              <div 
                class="battery-fill" 
                :style="{ width: status.batteryPercent + '%' }"
                :class="{ 
                  'battery-low': status.batteryPercent < 20,
                  'battery-charging': status.batteryCharging 
                }"
              >
                {{ status.batteryPercent }}%
              </div>
            </div>
          </div>
          <div class="status-item">
            <span class="label">Status:</span>
            <span class="value">
              {{ status.batteryCharging ? '⚡ Charging' : status.batteryStatus }}
            </span>
          </div>
        </div>
      </div>

      <!-- Performance -->
      <div class="status-card card">
        <h3>⚡ Performance</h3>
        <div class="status-items">
          <div class="status-item">
            <span class="label">FPS:</span>
            <span class="value">{{ status.fps }}</span>
          </div>
          <div class="status-item">
            <span class="label">Frame Count:</span>
            <span class="value mono">{{ status.frameCount.toLocaleString() }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { configApi } from '../api/config'
import type { HeadsetStatus } from '../types'

const status = ref<HeadsetStatus | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
let intervalId: number | null = null

async function loadStatus() {
  try {
    error.value = null
    status.value = await configApi.getHeadsetStatus()
    loading.value = false
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load status'
    loading.value = false
  }
}

async function handleResetPose() {
  if (!confirm('Recenter tracking? This will set your current position as the new origin (0,0,0).')) {
    return
  }
  
  try {
    await configApi.resetPose()
    await loadStatus() // Reload to show new position
  } catch (err) {
    alert('Failed to reset pose: ' + (err instanceof Error ? err.message : 'Unknown error'))
  }
}

onMounted(async () => {
  await loadStatus()
  // Update every second
  intervalId = setInterval(loadStatus, 1000) as unknown as number
})

onUnmounted(() => {
  if (intervalId !== null) {
    clearInterval(intervalId)
  }
})
</script>

<style scoped>
.status-view {
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

.error-message {
  padding: 1.5rem;
  background-color: rgba(220, 53, 69, 0.1);
  border: 1px solid var(--danger-color);
  border-radius: 8px;
  color: var(--danger-color);
  text-align: center;
}

.status-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
}

.status-card {
  padding: 1.5rem;
}

.status-card h3 {
  margin-bottom: 1rem;
  color: var(--primary-color);
  font-size: 1.1rem;
}

.status-items {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.status-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem;
  background-color: var(--bg-tertiary);
  border-radius: 6px;
}

.status-item .label {
  font-weight: 500;
  color: var(--text-secondary);
}

.status-item .value {
  font-weight: 600;
  color: var(--text-primary);
}

.status-item .value.mono {
  font-family: monospace;
  font-size: 0.95rem;
}

.status-item .value.success {
  color: var(--success-color);
}

.status-item .value.error {
  color: var(--danger-color);
}

.battery-bar {
  flex: 1;
  height: 28px;
  background-color: var(--bg-color);
  border-radius: 14px;
  overflow: hidden;
  margin-left: 1rem;
  border: 1px solid var(--border-color);
}

.battery-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--success-color), var(--primary-color));
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 0.85rem;
  color: #000;
  transition: width 0.3s ease;
}

.battery-fill.battery-low {
  background: linear-gradient(90deg, var(--danger-color), #ff6b6b);
}

.battery-fill.battery-charging {
  background: linear-gradient(90deg, var(--warning-color), var(--primary-color));
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.8; }
}

.reset-button {
  width: 100%;
  margin-top: 1rem;
  background-color: var(--warning-color);
  color: #000;
}

.reset-button:hover {
  background-color: #e0a800;
}

@media (max-width: 768px) {
  .status-grid {
    grid-template-columns: 1fr;
  }
}
</style>

