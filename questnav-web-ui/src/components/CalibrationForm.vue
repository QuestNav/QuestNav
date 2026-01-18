<template>
  <div class="calibration-form card">
    <h3>🧭 Calibration</h3>

    <div class="controls">
      <p>{{ step }}</p>
      <!-- Idle state -->
      <template v-if="step === 'Initial'">
        <button class="primary" @click="goToStep('CalibrateRotation')">Calibrate</button>
      </template>

      <!-- CalibrateRotation state -->
      <template v-else-if="step === 'CalibrateRotation'">
        <p class="acquiring">
          For best results:
          <ul>
            <li>Ensure the robot is on a level surface</li>
            <li>Move the robot forward 3m (10ft)</li>
            <li>Ensure the robot remains pointing forward</li>
          </ul>
        </p>
        <p class="summary">Distance traveled: {{ distanceTraveled.toFixed(2) }} m</p>
        <p class="summary">Starting Position: <{{ startPosition?.position.x.toFixed(3) }},
          {{ startPosition?.position.y.toFixed(3) }}, startPosition?.position.z.toFixed(3)}}></p>
        <p class="summary">Current Position: <{{ status?.position.x.toFixed(3) }}, {{ status?.position.y.toFixed(3) }},
          {{ status?.position.z.toFixed(3) }}></p>
        <button class="secondary" @click="resetRotation">Reset</button>
        <button v-if="distanceTraveled < MIN_DISTANCE_FOR_CALIBRATION" class="primary"
                @click="goToStep('CalibrateTranslation')">Skip
        </button>
        <button v-else class="primary" @click="goToStep('CalibrateTranslation')">Next</button>
      </template>

      <!-- CalibrateTranslation state -->
      <template v-else>
        <div class="acquiring">Acquiring Data</div>
        <button class="secondary" @click="resetTranslation">Reset</button>
        <button class="primary" @click="goToStep('Initial')"
                :disabled="samples?.length < MIN_SAMPLES_TO_COMPUTE">Finish
        </button>
        <div class="summary" v-if="samples?.length">
          <span>Samples: {{ samples.length }}</span>
        </div>

        <div v-if="samples?.length >= MIN_SAMPLES_TO_DRAW" class="canvas-wrapper">
          <canvas ref="canvasRef" :width="canvasSize" :height="canvasSize"></canvas>
          <div class="mean-label">
            Offset (x, y):
            <span class="mono">{{
                offset.x.toFixed(3)
              }}, {{ offset.y.toFixed(3) }}</span>
          </div>
          <button class="secondary" @click="copySamplesToClipboard" :disabled="copying">
            {{ copying ? 'Copying…' : 'Copy' }}
          </button>
        </div>
      </template>
    </div>

    <div class="summary">
      <pre>
        {{ rotationCode }}
        {{ translationCode }}
        public static final Pose3d questNavToRobot = new Transform3d(questToRobotTranslation, questToRobotRotation);
      </pre>
    </div>
  </div>
</template>

<script setup lang="ts">
import {ref, onMounted, onUnmounted, watch, computed, nextTick} from 'vue'
import {Euler, Quaternion, Vector3, toDegrees, Matrix3} from '@math.gl/core'
import type {HeadsetStatus} from '../types'
import {configApi} from '../api/config'
import {fit_circle, Point2D} from '../calibration'

const status = ref<HeadsetStatus | null>(null)
const error = ref<string | null>(null)
const copying = ref(false)

const canvasRef = ref<HTMLCanvasElement | null>(null)
const canvasSize = 320
const MIN_SAMPLES_TO_DRAW = 4
const MIN_SAMPLES_TO_COMPUTE = 10
const MIN_DISTANCE_FOR_CALIBRATION = 2.0

let pollId: number | null = null
let rotationEuler: Euler | null = null

// Generated code snippets
const rotationDecl = "public static final Rotation3D questToRobotRotation"
const rotationCode = ref<string>(`// Rotation offset not calibrated - ensure the following translation offset makes sense for your headset's orientation
${rotationDecl} = Rotation3d.kZero;`)
const translationDecl = "public static final Translation3D questToRobotTranslation";
const translationCode = ref<string>(`// Uncalibrated
${translationDecl} = Translation3d.kZero;`)

// Strong type for calibration sample points
type Sample = {
  // Pose
  position: Vector3
  rotation: Quaternion
  eulerAngles: Euler
}
const currentRotation = computed<Quaternion>(() => status.value ? new Quaternion(status.value.rotation.x, status.value.rotation.y, status.value.rotation.z, status.value.rotation.w)
    : new Quaternion())
const currentEulerAngles = computed<Euler>(() => new Euler().fromRotationMatrix(new Matrix3().fromQuaternion(currentRotation.value), Euler.XYZ))

type Step = 'Initial' | 'CalibrateRotation' | 'CalibrateTranslation'
const step = ref<Step>('Initial')

// Initial state
async function enterInitial() {
  // no-op
}

async function exitInitial() {
  // no-op
}

// Rotation (yaw) Calibration
// Rotation calibration state data
const startPosition = ref<Sample | null>(null)
const distanceTraveled = computed(() => {
  if (!startPosition.value || !status.value) return 0
  const dx = status.value.position.x - startPosition.value.position.x
  const dy = status.value.position.y - startPosition.value.position.y
  return Math.hypot(dx, dy)
})

async function resetRotation() {
  await configApi.resetPose()
  await loadStatus()
  startPosition.value = status.value ? {
    position: new Vector3(status.value.position.x, status.value.position.y, status.value.position.z),
    rotation: currentRotation.value,
    eulerAngles: currentEulerAngles.value
  } : null
}

async function enterRotation() {
  await resetRotation()
}

async function exitRotation() {
  await loadStatus()
  if (!startPosition.value || !status.value) return
  const curPos = new Vector3(status.value.position.x, status.value.position.y, status.value.position.z)
  const startPosVec = new Vector3(startPosition.value.position.x, startPosition.value.position.y, startPosition.value.position.z)
  const forward = curPos.addScaledVector(startPosVec, -1).normalize()
  // Recentering orients the headset with forward = +Z.
  // Subtract a quarter rotation from the yaw to account for this.
  const yaw = Math.atan2(forward.y, forward.x)
  if (Math.abs(forward.z) > 0.1) {
    error.value = `Robot is not level - moved ${forward.z.toFixed(3)}. Rotation may be incorrect.`
  }

  console.log({forward, yaw: toDegrees(yaw)})
  const initialEulers = startPosition.value.eulerAngles
  const yawDegrees = toDegrees(yaw)
  rotationEuler = new Euler(0, 0, yaw)
  rotationCode.value =
      `
        // The complete Euler angles roll & pitch of the headset relative to the ground plane are included below.
        // For headsets mounted upright and relatively level only yaw typically matters.
        //   pitch = ${initialEulers.pitch.toFixed(3)}
        //   roll = ${initialEulers.roll.toFixed(3)}
        //   yaw = ${yawDegrees.toFixed(3)}
        // Represents the yaw offset of the headset relative to the robot's forward direction
        ${rotationDecl} = new Rotation3d(Degrees.of(0), Degrees.of(0), Degrees.of(${yawDegrees.toFixed(3)})");
        `
}

// Translation Calibration
// Translation Calibration data and logic
let translationPollId: null | number = null
const samples = ref<Sample[]>([])
const lastEuler = ref<Euler | null>(null)

const geoMedianValue = computed<Point2D>(() => fit_circle(samples.value.map(p => {
  return {x: p.position.x, y: p.position.y} as Point2D
})))

const meanRadius = computed(() => {
  const pts: Sample[] = samples.value
  if (pts.length === 0) return 0
  const c = geoMedianValue.value
  const sum = pts.reduce((acc, p) => acc + Math.hypot(p.position.x - c.x, p.position.y - c.y), 0)
  return sum / pts.length
})

const offset = computed<Point2D>(() => ({
  x: -geoMedianValue.value.x,
  y: -geoMedianValue.value.y,
}))

function drawSamples() {
  const canvas = canvasRef.value
  const pts: Sample[] = samples.value
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
  const toCanvas = (p: Point2D) => ({
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
    const q = toCanvas(p.position)
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

async function copySamplesToClipboard() {
  const s = samples.value
  if (!s?.length) return
  const points: Point2D[] = s.map(p => {
    return {x: p.position.x, y: p.position.y} as Point2D
  })
  const lines = points.map(p => `${p.x},${p.y}`)
  const csv = lines.join('\n')
  copying.value = true
  try {
    if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
      await navigator.clipboard.writeText(csv)
    } else {
      const textarea = document.createElement('textarea')
      textarea.value = csv
      textarea.style.position = 'fixed'
      textarea.style.left = '-9999px'
      document.body.appendChild(textarea)
      textarea.focus()
      textarea.select()
      try {
        document.execCommand('copy')
      } catch {
      }
      document.body.removeChild(textarea)
    }
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to copy to clipboard'
  } finally {
    copying.value = false
  }
}

async function resetTranslation() {
  samples.value = []
  lastEuler.value = null

  // Reset the pose to 0,0,0 with the yaw calculated above before calculating the XY offset
  try {
    await configApi.resetPose(
        {x: 0, y: 0, z: 0},
        rotationEuler ? {pitch: rotationEuler.y, roll: rotationEuler.x, yaw: rotationEuler.z} : undefined)
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to reset pose'
  }
}

async function sample_rotation() {
  if (!status.value) return
  const currEuler = new Euler(status.value.eulerAngles.roll, status.value.eulerAngles.pitch, status.value.eulerAngles.yaw, Euler.YZX)
  if (!lastEuler.value) {
    lastEuler.value = currEuler
    return
  }
  const delta = eulerDeltaDeg(currEuler, lastEuler.value)
  if (delta >= 15) {
    lastEuler.value = currEuler
    const currQuat = currEuler.toQuaternion()
    const pt: Sample = {
      position: new Vector3(status.value.position.x, status.value.position.y, status.value.position.z),
      rotation: currQuat,
      eulerAngles: currEuler
    }
    samples.value.push(pt)
  }
}

let unwatchSamples: null | (() => void) = null

async function enterTranslation() {
  await resetTranslation()
  translationPollId = setInterval(sample_rotation, 1000) as number
  unwatchSamples = watch(samples, async (v: Sample[]) => {
    if (v.length >= MIN_SAMPLES_TO_COMPUTE) {
      await nextTick()
      drawSamples()
    }
  }, {deep: true})
}

function set_translation_offset() {
  const center = geoMedianValue.value
  if (!center) {
    console.error("Center not found - translation offset not computed", {
      samples,
      center,
      geoMedianValue
    })
    return
  }

  const z_offset = status.value?.position.z ?? 0
  translationCode.value = `
  // z value is the headset's estimated distance to the floor
  ${translationDecl} = new Translation3d(${center.x.toFixed(3)}, ${center.y.toFixed(3)}, ${z_offset.toFixed(3)});`
}

async function exitTranslation() {
  set_translation_offset()
  if (translationPollId) {
    clearInterval(translationPollId)
    translationPollId = null
  }
  if (unwatchSamples) {
    unwatchSamples()
    unwatchSamples = null
  }
}

async function goToStep(next: Step) {
  // exit current
  switch (step.value) {
    case 'Initial':
      await exitInitial()
      break;
    case 'CalibrateRotation':
      await exitRotation()
      break;
    case 'CalibrateTranslation':
      await exitTranslation()
      break;
  }
  // enter next
  step.value = next
  switch (next) {
    case 'Initial':
      await enterInitial()
      break;
    case 'CalibrateRotation':
      await enterRotation()
      break;
    case 'CalibrateTranslation':
      await enterTranslation()
      break;
  }
}

async function loadStatus() {
  try {
    status.value = await configApi.getHeadsetStatus()
  } catch (e: any) {
    error.value = e?.message ?? 'Failed to load headset status'
  }
}

function eulerDeltaDeg(a: { pitch: number; yaw: number; roll: number }, b: {
  pitch: number;
  yaw: number;
  roll: number
}) {
  const dx = Math.abs(a.pitch - b.pitch)
  const dy = Math.abs(a.yaw - b.yaw)
  const dz = Math.abs(a.roll - b.roll)
  return Math.max(dx, dy, dz)
}

onMounted(async () => {
  await loadStatus()
  pollId = setInterval(async () => {
    await loadStatus()
  }, 1000) as number
})

onUnmounted(() => {
  if (pollId) clearInterval(pollId)
})

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
  border: 1px solid rgba(255, 255, 255, 0.15);
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
}

.acquiring {
  color: #ffd166;
  font-weight: 600;
}

.summary {
  color: #9aa3b2;
}

.canvas-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

canvas {
  border-radius: 12px;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.06);
}

.mono {
  font-family: ui-monospace, Menlo, Consolas, monospace;
}

.mean-label {
  color: #cbd5e1;
}
</style>
