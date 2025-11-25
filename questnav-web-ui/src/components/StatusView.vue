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
            <span class="label">Robot Connection:</span>
            <span :class="['value', status.networkConnected ? 'success' : 'error']">
              {{ status.networkConnected ? 'Connected' : 'Disconnected' }}
            </span>
          </div>
          <div class="status-item">
            <span class="label">Robot IP Address:</span>
            <span class="value mono">{{ status.robotIpAddress || 'N/A' }}</span>
          </div>
          <div class="status-item">
            <span class="label">Headset IP Address:</span>
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
            <span class="label">Yaw:</span>
            <span class="value mono">{{ status.eulerAngles.pitch.toFixed(1) }}°</span>
          </div>
          <div class="status-item">
            <span class="label">Pitch:</span>
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
          <div class="status-item">
            <span class="label">Connected Clients:</span>
            <span class="value">{{ status.connectedClients }}</span>
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

.status-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
  gap: 1.5rem;
}

.status-card {
  padding: 1.75rem;
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, rgba(0, 0, 0, 0.2) 100%);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.status-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, var(--primary-color), var(--teal));
  opacity: 0;
  transition: opacity 0.3s ease;
}

.status-card:hover {
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
}

.status-card:hover::before {
  opacity: 1;
}

.status-card h3 {
  margin-bottom: 1.25rem;
  color: var(--primary-color);
  font-size: 1.3rem;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-items {
  display: flex;
  flex-direction: column;
  gap: 0.875rem;
}

.status-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: white;
  border-radius: 8px;
  border-left: 3px solid transparent;
  transition: all 0.2s ease;
  border: 1px solid var(--border-color);
}

.status-item:hover {
  background: var(--bg-tertiary);
  border-left-color: var(--primary-color);
  transform: translateX(3px);
}

.status-item .label {
  font-weight: 600;
  color: var(--text-secondary);
  font-size: 0.95rem;
}

.status-item .value {
  font-weight: 700;
  color: var(--text-primary);
  font-size: 1rem;
}

.status-item .value.mono {
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 0.95rem;
  background: var(--bg-tertiary);
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
}

.status-item .value.success {
  color: var(--success-color);
  text-shadow: 0 0 8px rgba(76, 175, 80, 0.4);
}

.status-item .value.error {
  color: var(--danger-color);
  text-shadow: 0 0 8px rgba(220, 53, 69, 0.4);
}

.battery-bar {
  flex: 1;
  height: 32px;
  background-color: #E5E7EB;
  border-radius: 16px;
  overflow: hidden;
  margin-left: 1rem;
  border: 2px solid var(--border-color);
  box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.1);
}

.battery-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--success-color), var(--primary-color));
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
  font-size: 0.9rem;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
  transition: width 0.5s ease;
  position: relative;
}

.battery-fill::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: linear-gradient(to bottom, rgba(255, 255, 255, 0.2), transparent);
  pointer-events: none;
}

.battery-fill.battery-low {
  background: linear-gradient(90deg, var(--danger-color), #ff6b6b);
  animation: batteryPulse 2s ease-in-out infinite;
}

.battery-fill.battery-charging {
  background: linear-gradient(90deg, var(--warning-color), var(--primary-color));
  animation: chargingShine 2s linear infinite;
}

@keyframes batteryPulse {
  0%, 100% { 
    opacity: 1;
    box-shadow: 0 0 0 rgba(220, 53, 69, 0);
  }
  50% { 
    opacity: 0.8;
    box-shadow: 0 0 20px rgba(220, 53, 69, 0.5);
  }
}

@keyframes chargingShine {
  0% {
    background-position: -200% 0;
  }
  100% {
    background-position: 200% 0;
  }
}

.reset-button {
  width: 100%;
  margin-top: 1.25rem;
  background: linear-gradient(135deg, var(--warning-color), #ffa000);
  color: #000;
  font-weight: 700;
  padding: 0.75rem 1rem;
  box-shadow: 0 4px 12px rgba(255, 193, 7, 0.3);
}

.reset-button:hover {
  background: linear-gradient(135deg, #e0a800, #ff8f00);
  box-shadow: 0 6px 16px rgba(255, 193, 7, 0.4);
}

@media (max-width: 768px) {
  .status-grid {
    grid-template-columns: 1fr;
  }
}
</style>

