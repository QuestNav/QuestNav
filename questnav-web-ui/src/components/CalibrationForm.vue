<template>
  <div class="calibration-form card">
    <h3>🧭 Calibration</h3>

    <div class="controls">
      <button v-if="!isCalibrating" class="primary" @click="beginCalibration">
        Begin Calibration
      </button>
      <template v-else>
        <div class="acquiring">Acquiring Data</div>
        <button class="primary" @click="endCalibration">End Calibration</button>
      </template>
      <div class="summary" v-if="samples.length > 0">
        <span>Samples: {{ samples.length }}</span>
      </div>
      <button v-if="samples.length > 0" class="secondary" @click="copySamples" :disabled="copying">
        {{ copying ? 'Copying…' : 'Copy' }}
      </button>
    </div>

    <div v-if="samples.length >= 4" class="canvas-wrapper">
      <canvas ref="canvasRef" :width="canvasSize" :height="canvasSize"></canvas>
      <div class="mean-label">
        Offset (x, y):
        <span class="mono">{{ offset.x.toFixed(3) }}, {{ offset.y.toFixed(3) }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch, computed, nextTick } from 'vue'
import type { HeadsetStatus } from '../types'
import { configApi } from '../api/config'
import { fit_circle } from '../calibration'

// Strong type for calibration sample points. Do not replace with Point2D - we will need to add rotation in the future
// to compute the full relative transform.
type Sample = { x: number; y: number }

const isCalibrating = ref(false)
const status = ref<HeadsetStatus | null>(null)
const error = ref<string | null>(null)
const lastEuler = ref<{ pitch: number; yaw: number; roll: number } | null>(null)
const samples = ref<Sample[]>([])
const copying = ref(false)

const canvasRef = ref<HTMLCanvasElement | null>(null)
const canvasSize = 320

let pollId: number | null = null

async function loadStatus() {
  try {
    status.value = await configApi.getHeadsetStatus()
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to load headset status'
  }
}

function eulerDeltaDeg(a: { pitch: number; yaw: number; roll: number }, b: { pitch: number; yaw: number; roll: number }) {
  const dx = Math.abs(a.pitch - b.pitch)
  const dy = Math.abs(a.yaw - b.yaw)
  const dz = Math.abs(a.roll - b.roll)
  return Math.max(dx, dy, dz)
}

async function beginCalibration() {
  samples.value = []
  lastEuler.value = null
  isCalibrating.value = true
  try {
    // Reset pose at the start of calibration
    await configApi.resetPose()
  } catch (e: any) {
    // Surface but do not block calibration UI
    error.value = e?.message ?? 'Failed to reset pose'
  }
  // Refresh status once after resetting pose
  await loadStatus()
}

function endCalibration() {
  // Stop acquiring new samples but keep existing samples and visualization
  isCalibrating.value = false
}

async function copySamples() {
  if (!samples.value.length) return
  const lines = samples.value.map(p => `${p.x},${p.y}`)
  const csv = lines.join('\n')
  copying.value = true
  try {
    if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
      await navigator.clipboard.writeText(csv)
    } else {
      // Fallback for non-secure contexts (e.g., Unity WebView)
      const textarea = document.createElement('textarea')
      textarea.value = csv
      textarea.style.position = 'fixed'
      textarea.style.left = '-9999px'
      document.body.appendChild(textarea)
      textarea.focus()
      textarea.select()
      try { document.execCommand('copy') } catch {}
      document.body.removeChild(textarea)
    }
  } catch (e: any) {
    // Surface an error if copy failed
    error.value = e?.message ?? 'Failed to copy to clipboard'
  } finally {
    copying.value = false
  }
}

// Polling loop like StatusView
onMounted(async () => {
  await loadStatus()
  pollId = setInterval(async () => {
    await loadStatus()
    if (isCalibrating.value && status.value) {
      const currEuler = status.value.eulerAngles
      if (!lastEuler.value) {
        // Initialize comparison baseline
        lastEuler.value = { ...currEuler }
        return
      }
      const delta = eulerDeltaDeg(currEuler, lastEuler.value)
      if (delta >= 15) {
        lastEuler.value = { ...currEuler }
        const pt: Sample = {
          x: status.value.position.x,
          y: status.value.position.y
        }
        samples.value.push(pt)
        // Redraw if we have canvas visible
        if (samples.value.length >= 4) {
          await nextTick()
          draw()
        }
      }
    }
  }, 1000) as unknown as number
})

onUnmounted(() => {
  if (pollId) clearInterval(pollId)
})

// Use external geo_median calculation from calibration utilities
const geoMedianValue = computed<Sample>(() => fit_circle(samples.value as Sample[]) as Sample)

const meanRadius = computed(() => {
  const pts : Sample[] = samples.value
  if (pts.length === 0) return 0
  const c = geoMedianValue.value
  const sum = pts.reduce((acc, p) => acc + Math.hypot(p.x - c.x, p.y - c.y), 0)
  return sum / pts.length
})

// Display offset as the negative of the geometric median
const offset = computed<Sample>(() => ({
  x: -geoMedianValue.value.x,
  y: -geoMedianValue.value.y,
}))

watch(samples, async (v: Sample[]) => {
  if (v.length >= 10) {
    await nextTick()
    draw()
  }
}, { deep: true })

function draw() {
  const canvas = canvasRef.value
  const pts : Sample[] = samples.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')!
  const size = canvasSize
  ctx.clearRect(0, 0, size, size)

  // Grid background
  ctx.fillStyle = '#0b1020'
  ctx.fillRect(0, 0, size, size)

  // Fixed world scale: plot within X∈[-1,1] (forward), Y∈[-1,1] (left)
  // Keep origin (0,0) at canvas center; add margin around edges
  const margin = 20
  const halfRange = 1 // meters
  const scale = (size / 2 - margin) / halfRange

  // Helper to convert world (meters) to canvas pixels with fixed scaling
  // World: +X is forward (up on canvas), +Y is left (left on canvas)
  // Canvas: +X right, +Y down
  const toCanvas = (p: Sample) => ({
    x: size / 2 - p.y * scale,
    y: size / 2 - p.x * scale,
  })

  // Draw minor grid
  ctx.strokeStyle = '#1e2748'
  ctx.lineWidth = 1
  const gridStepPx = 20
  for (let x = gridStepPx; x < size; x += gridStepPx) {
    ctx.beginPath()
    ctx.moveTo(x, 0)
    ctx.lineTo(x, size)
    ctx.stroke()
  }
  for (let y = gridStepPx; y < size; y += gridStepPx) {
    ctx.beginPath()
    ctx.moveTo(0, y)
    ctx.lineTo(size, y)
    ctx.stroke()
  }

  // Axes
  ctx.strokeStyle = '#3b4e9a'
  ctx.lineWidth = 2
  ctx.beginPath()
  ctx.moveTo(0, size / 2)
  ctx.lineTo(size, size / 2)
  ctx.stroke()
  ctx.beginPath()
  ctx.moveTo(size / 2, 0)
  ctx.lineTo(size / 2, size)
  ctx.stroke()

  // Axis labels: only min/max values at extremes for fixed scale [-1,1]
  ctx.fillStyle = '#9aa3b2'
  ctx.font = '12px ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial'

  // Vertical axis (X world): top = +1, bottom = -1
  ctx.textAlign = 'center'
  ctx.textBaseline = 'top'
  ctx.fillText('Forward (+1)', size / 2, 6)
  ctx.textBaseline = 'bottom'
  ctx.fillText('-1', size / 2, size - 4)

  // Horizontal axis (Y world): left = +1, right = -1
  ctx.textAlign = 'left'
  ctx.textBaseline = 'middle'
  ctx.fillText('Left (+1)', 6, size / 2)
  ctx.textAlign = 'right'
  ctx.fillText('-1', size - 4, size / 2)

  // Points
  for (const p of pts) {
    const q = toCanvas(p)
    ctx.fillStyle = '#ffd166'
    ctx.beginPath()
    ctx.arc(q.x, q.y, 2, 0, Math.PI * 2)
    ctx.fill()
  }

  // Mean-radius circle rendered on top of points, centered at geometric median
  const c = geoMedianValue.value
  const mc = toCanvas(c)

  // Plot the median point
  ctx.fillStyle = '#22d3ee'
  ctx.beginPath()
  ctx.arc(mc.x, mc.y, 2, 0, Math.PI * 2)
  ctx.fill()

  // Plot the mean-radius circle if it's large enough to be visible'
  const rPx = meanRadius.value * scale
  if (rPx > 5) {
    ctx.strokeStyle = '#22d3ee'
    ctx.lineWidth = 2
    ctx.beginPath()
    ctx.arc(mc.x, mc.y, rPx, 0, Math.PI * 2)
    ctx.stroke()
  }
}
</script>

<style scoped>
.calibration-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.controls {
  display: flex;
  align-items: center;
  gap: 12px;
}
.primary {
  background: linear-gradient(135deg, #6366f1, #3b82f6);
  color: white;
  border: none;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
}
.secondary {
  background: transparent;
  color: #cbd5e1;
  border: 1px solid rgba(255,255,255,0.15);
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
}
.acquiring {
  color: #ffd166;
  font-weight: 600;
}
.summary { color: #9aa3b2; }
.canvas-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}
canvas {
  border-radius: 12px;
  box-shadow: inset 0 0 0 1px rgba(255,255,255,0.06);
}
.mono { font-family: ui-monospace, Menlo, Consolas, monospace; }
.mean-label { color: #cbd5e1; }
</style>
