<template>
  <div class="config-field" :class="{ 
    'field-warning': isDebugIPField && localValue,
    'field-disabled': isTeamNumberField && isDebugIPActive
  }">
    <div class="field-header">
      <label :for="field.path" class="field-label">
        {{ field.displayName }}
        <span v-if="field.requiresRestart" class="restart-badge">Requires Restart</span>
        <span v-if="isDebugIPField && localValue" class="debug-badge">DEBUG OVERRIDE ENABLED</span>
        <span v-if="isTeamNumberField && isDebugIPActive" class="override-badge">OVERRIDDEN</span>
      </label>
      <span v-if="field.description" class="field-description">
        {{ field.description }}
      </span>
      <div v-if="isDebugIPField && localValue" class="debug-warning">
        WARNING: Team number is being overridden. Connection will use IP: {{ localValue }}
      </div>
      <div v-if="isTeamNumberField && isDebugIPActive" class="override-notice">
        This setting is currently overridden by Debug IP Override
      </div>
    </div>

    <div class="field-control">
      <!-- Checkbox -->
      <div v-if="field.controlType === 'checkbox'" class="checkbox-control">
        <input
          :id="field.path"
          type="checkbox"
          :checked="localValue"
          @change="handleCheckboxChange"
        />
        <span class="checkbox-label">{{ localValue ? 'Enabled' : 'Disabled' }}</span>
      </div>

      <!-- Slider -->
      <div v-else-if="field.controlType === 'slider'" class="slider-control">
        <input
          :id="field.path"
          type="range"
          :min="field.min"
          :max="field.max"
          :step="field.step || 1"
          :value="localValue"
          @input="handleSliderInput"
        />
        <div class="slider-value">
          <input
            type="number"
            :min="field.min"
            :max="field.max"
            :step="field.step || 1"
            :value="localValue"
            @input="handleNumberInput"
            class="value-input"
          />
        </div>
      </div>

      <!-- Color Picker -->
      <div v-else-if="field.controlType === 'color'" class="color-control">
        <input
          :id="field.path"
          type="color"
          :value="colorToHex(localValue)"
          @input="handleColorInput"
        />
        <div class="color-values">
          <span class="color-value">{{ colorToHex(localValue) }}</span>
          <span class="color-value">RGBA: {{ formatColorRGBA(localValue) }}</span>
        </div>
      </div>

      <!-- Select / Dropdown -->
      <div v-else-if="field.controlType === 'select' && field.options" class="select-control">
        <select
          :id="field.path"
          :value="localValue"
          @change="handleSelectChange"
        >
          <option v-for="option in field.options" :key="option" :value="option">
            {{ option }}
          </option>
        </select>
      </div>

      <!-- Text Input (default) -->
      <div v-else class="input-control">
        <input
          :id="field.path"
          :type="getInputType(field.type)"
          :value="localValue"
          @input="handleTextInput"
          @keyup.enter="handleSubmit"
        />
        <button 
          v-if="(isTeamNumberField && hasTeamNumberChanged) || (isDebugIPField && hasDebugIPChanged)"
          @click="handleSubmit"
          class="submit-button"
        >
          Apply
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, inject } from 'vue'
import type { ConfigFieldSchema } from '../types'

const props = defineProps<{
  field: ConfigFieldSchema
  value: any
  debugIPOverride?: string
}>()

const emit = defineEmits<{
  update: [path: string, value: any]
}>()

const localValue = ref(props.value)
const pendingTeamNumber = ref<number | null>(null)
const pendingDebugIP = ref<string | null>(null)

const isDebugIPField = computed(() => props.field.path === 'WebServerConstants/debugNTServerAddressOverride')
const isTeamNumberField = computed(() => props.field.path === 'WebServerConstants/webConfigTeamNumber')
const isDebugIPActive = computed(() => {
  return props.debugIPOverride !== undefined && props.debugIPOverride !== null && props.debugIPOverride !== ''
})
const hasTeamNumberChanged = computed(() => {
  return isTeamNumberField.value && pendingTeamNumber.value !== null && pendingTeamNumber.value !== props.value
})
const hasDebugIPChanged = computed(() => {
  return isDebugIPField.value && pendingDebugIP.value !== null && pendingDebugIP.value !== props.value
})

watch(() => props.value, (newValue) => {
  localValue.value = newValue
  if (isTeamNumberField.value) {
    pendingTeamNumber.value = null // Reset pending when server value changes
  }
  if (isDebugIPField.value) {
    pendingDebugIP.value = null // Reset pending when server value changes
  }
})

function handleCheckboxChange(event: Event) {
  const target = event.target as HTMLInputElement
  localValue.value = target.checked
  emit('update', props.field.path, target.checked)
}

function handleSliderInput(event: Event) {
  const target = event.target as HTMLInputElement
  const value = props.field.type === 'int' 
    ? parseInt(target.value)
    : parseFloat(target.value)
  localValue.value = value
  emit('update', props.field.path, value)
}

function handleNumberInput(event: Event) {
  const target = event.target as HTMLInputElement
  const value = props.field.type === 'int'
    ? parseInt(target.value)
    : parseFloat(target.value)
  
  if (!isNaN(value)) {
    localValue.value = value
    emit('update', props.field.path, value)
  }
}

function handleColorInput(event: Event) {
  const target = event.target as HTMLInputElement
  const hex = target.value
  const color = hexToColor(hex)
  localValue.value = color
  emit('update', props.field.path, color)
}

function handleSelectChange(event: Event) {
  const target = event.target as HTMLSelectElement
  localValue.value = target.value
  emit('update', props.field.path, target.value)
}

function handleTextInput(event: Event) {
  const target = event.target as HTMLInputElement
  localValue.value = target.value
  
  // For Team Number field, store pending value but don't emit yet
  if (isTeamNumberField.value) {
    const numValue = parseInt(target.value)
    if (!isNaN(numValue)) {
      pendingTeamNumber.value = numValue
    }
    // Don't emit - wait for submit button
    return
  }
  
  // For Debug IP Override field, store pending value but don't emit yet
  if (isDebugIPField.value) {
    pendingDebugIP.value = target.value
    // Don't emit - wait for submit button
    return
  }
  
  // For other text fields, emit immediately
  emit('update', props.field.path, target.value)
}

function handleSubmit() {
  if (isTeamNumberField.value && pendingTeamNumber.value !== null) {
    emit('update', props.field.path, pendingTeamNumber.value)
    pendingTeamNumber.value = null
  } else if (isDebugIPField.value && pendingDebugIP.value !== null) {
    // Validate before submitting
    if (isValidIPOrHostname(pendingDebugIP.value) || pendingDebugIP.value === '') {
      emit('update', props.field.path, pendingDebugIP.value)
      pendingDebugIP.value = null
    } else {
      // Show error or don't submit invalid IP
      alert('Please enter a valid IP address, hostname, or leave empty')
    }
  }
}

function isValidIPOrHostname(value: string): boolean {
  if (!value || value.trim() === '') return true
  
  const trimmed = value.trim()
  
  // Check for complete IPv4 (xxx.xxx.xxx.xxx)
  const ipv4Regex = /^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/
  const ipv4Match = trimmed.match(ipv4Regex)
  if (ipv4Match) {
    // Validate each octet is 0-255
    for (let i = 1; i <= 4; i++) {
      const octet = parseInt(ipv4Match[i])
      if (octet < 0 || octet > 255) return false
    }
    return true
  }
  
  // Check for IPv6 (basic check - contains colons and hex)
  const ipv6Regex = /^([0-9a-fA-F]{0,4}:){2,7}[0-9a-fA-F]{0,4}$/
  if (ipv6Regex.test(trimmed)) {
    return true
  }
  
  // Check for hostname (alphanumeric with dots, hyphens, underscores)
  const hostnameRegex = /^[a-zA-Z0-9]([a-zA-Z0-9\-\._]{0,251}[a-zA-Z0-9])?$/
  if (hostnameRegex.test(trimmed)) {
    return true
  }
  
  return false
}

function getInputType(type: string): string {
  if (type === 'int' || type === 'float' || type === 'double') return 'number'
  return 'text'
}

function colorToHex(color: any): string {
  if (!color) return '#ffffff'
  
  if (typeof color === 'string') return color
  
  if (typeof color === 'object' && 'r' in color) {
    const r = Math.round(color.r * 255).toString(16).padStart(2, '0')
    const g = Math.round(color.g * 255).toString(16).padStart(2, '0')
    const b = Math.round(color.b * 255).toString(16).padStart(2, '0')
    return `#${r}${g}${b}`
  }
  
  return '#ffffff'
}

function hexToColor(hex: string): any {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex)
  
  if (!result) return { r: 1, g: 1, b: 1, a: 1 }
  
  return {
    r: parseInt(result[1], 16) / 255,
    g: parseInt(result[2], 16) / 255,
    b: parseInt(result[3], 16) / 255,
    a: typeof localValue.value === 'object' && 'a' in localValue.value 
      ? localValue.value.a 
      : 1
  }
}

function formatColorRGBA(color: any): string {
  if (!color || typeof color !== 'object') return 'N/A'
  
  const r = Math.round((color.r || 0) * 255)
  const g = Math.round((color.g || 0) * 255)
  const b = Math.round((color.b || 0) * 255)
  const a = (color.a || 1).toFixed(2)
  
  return `${r}, ${g}, ${b}, ${a}`
}
</script>

<style scoped>
.config-field {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.5rem;
  background: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  transition: all 0.2s ease;
}

.config-field:hover {
  border-color: var(--primary-color);
  box-shadow: 0 4px 12px rgba(51, 161, 253, 0.15);
}

.config-field.field-warning {
  background: rgba(255, 193, 7, 0.1);
  border: 2px solid var(--warning-color);
}

.config-field.field-disabled {
  opacity: 0.6;
  pointer-events: none;
  background: var(--bg-tertiary);
  border-color: var(--text-muted);
}

.field-header {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.field-label {
  font-weight: 600;
  font-size: 0.875rem; /* Small text: 14px */
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.restart-badge {
  font-size: 0.75rem; /* Extra small: 12px */
  padding: 0.25rem 0.65rem;
  background: var(--warning-color);
  color: #000;
  border-radius: 12px;
  border: 2px solid var(--amber-dark);
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 4px rgba(255, 193, 7, 0.3);
}

.debug-badge {
  font-size: 0.75rem; /* Extra small: 12px */
  padding: 0.25rem 0.65rem;
  background: var(--danger-color);
  color: #fff;
  border-radius: 12px;
  border: 2px solid #c82333;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 4px rgba(220, 53, 69, 0.3);
}

.override-badge {
  font-size: 0.75rem; /* Extra small: 12px */
  padding: 0.25rem 0.65rem;
  background: var(--secondary-color);
  color: #fff;
  border-radius: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.override-notice {
  margin-top: 0.5rem;
  padding: 0.875rem;
  background: rgba(108, 117, 125, 0.1);
  border-left: 3px solid var(--secondary-color);
  border-radius: 6px;
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 0.875rem; /* Small text: 14px */
  line-height: 1.5;
}

.debug-warning {
  margin-top: 0.5rem;
  padding: 0.875rem;
  background: rgba(255, 193, 7, 0.1);
  border-left: 3px solid var(--warning-color);
  border-radius: 6px;
  color: var(--text-primary);
  font-weight: 600;
  font-size: 0.875rem; /* Small text: 14px */
  line-height: 1.5;
}

.field-description {
  font-size: 0.875rem; /* Small text: 14px */
  color: var(--text-secondary);
  line-height: 1.6;
}

.field-control {
  width: 100%;
}

.checkbox-control {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: var(--bg-tertiary);
  border-radius: 6px;
  border: 1px solid transparent;
  transition: all 0.2s ease;
}

.checkbox-control:hover {
  border-color: var(--primary-color);
}

.checkbox-control input[type="checkbox"] {
  width: 1.5rem;
  height: 1.5rem;
  cursor: pointer;
}

.checkbox-label {
  font-weight: 600;
  font-size: 0.875rem; /* Small text: 14px */
  color: var(--text-primary);
}

.slider-control {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.slider-control input[type="range"] {
  height: 6px;
  border-radius: 3px;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
}

.slider-value {
  display: flex;
  justify-content: flex-end;
}

.value-input {
  width: 120px;
  text-align: center;
  font-weight: 600;
  font-size: 1rem; /* Base text: 16px */
  padding: 0.6rem;
  background: var(--input-bg);
  border: 1px solid var(--border-color);
  transition: all 0.2s ease;
}

.value-input:hover {
  border-color: var(--primary-color);
}

.value-input:focus {
  border-color: var(--primary-color);
  box-shadow: 0 0 0 3px rgba(51, 161, 253, 0.1);
}

.color-control {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem;
  background: var(--bg-tertiary);
  border-radius: 6px;
}

.color-control input[type="color"] {
  width: 80px;
  height: 50px;
  border: 2px solid var(--border-color);
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.color-control input[type="color"]:hover {
  border-color: var(--primary-color);
}

.color-values {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.color-value {
  font-size: 0.875rem; /* Small text: 14px */
  color: var(--text-secondary);
  font-weight: 500;
}

.select-control select {
  width: 100%;
  padding: 0.75rem;
  background: var(--input-bg);
  border: 1px solid var(--border-color);
  font-size: 0.875rem; /* Small text: 14px */
  font-weight: 500;
  transition: all 0.2s ease;
}

.select-control select:hover {
  border-color: var(--primary-color);
}

.select-control select:focus {
  box-shadow: 0 0 0 3px rgba(51, 161, 253, 0.1);
}

.input-control {
  display: flex;
  gap: 0.75rem;
  align-items: stretch;
}

.input-control input {
  flex: 1;
  padding: 0.75rem;
  background: var(--input-bg);
  border: 1px solid var(--border-color);
  font-size: 0.875rem; /* Small text: 14px */
  font-weight: 500;
  transition: all 0.2s ease;
}

.submit-button {
  padding: 0.75rem 1.5rem;
  background: var(--primary-color);
  color: white;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  font-size: 0.875rem; /* Small text: 14px */
  cursor: pointer;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.submit-button:hover {
  background: var(--primary-dark);
}

.submit-button:active {
  transform: translateY(0);
}

.input-control input:hover {
  border-color: var(--primary-color);
}

.input-control input:focus {
  box-shadow: 0 0 0 3px rgba(51, 161, 253, 0.1);
}
</style>

