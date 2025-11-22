<template>
  <div class="config-field" :class="{ 'field-warning': isDebugIPField && localValue }">
    <div class="field-header">
      <label :for="field.path" class="field-label">
        {{ field.displayName }}
        <span v-if="field.requiresRestart" class="restart-badge">Requires Restart</span>
        <span v-if="isDebugIPField && localValue" class="debug-badge">DEBUG MODE ACTIVE</span>
      </label>
      <span v-if="field.description" class="field-description">
        {{ field.description }}
      </span>
      <div v-if="isDebugIPField && localValue" class="debug-warning">
        WARNING: Team number is being overridden. Connection will use IP: {{ localValue }}
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
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { ConfigFieldSchema } from '../types'

const props = defineProps<{
  field: ConfigFieldSchema
  value: any
}>()

const emit = defineEmits<{
  update: [path: string, value: any]
}>()

const localValue = ref(props.value)

const isDebugIPField = computed(() => props.field.path === 'Tunables/debugNTServerAddressOverride')

watch(() => props.value, (newValue) => {
  localValue.value = newValue
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
  emit('update', props.field.path, target.value)
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
  background: linear-gradient(135deg, var(--bg-tertiary) 0%, rgba(0, 0, 0, 0.2) 100%);
  border: 1px solid var(--border-color);
  border-radius: 12px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.config-field::before {
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

.config-field:hover {
  border-color: var(--primary-color);
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
}

.config-field:hover::before {
  opacity: 1;
}

.config-field.field-warning {
  background: linear-gradient(135deg, rgba(255, 193, 7, 0.15) 0%, rgba(255, 193, 7, 0.05) 100%);
  border: 2px solid var(--warning-color);
  box-shadow: 0 4px 20px rgba(255, 193, 7, 0.2);
}

.config-field.field-warning::before {
  background: var(--warning-color);
  opacity: 1;
}

.field-header {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.field-label {
  font-weight: 700;
  font-size: 1.05rem;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.restart-badge {
  font-size: 0.7rem;
  padding: 0.25rem 0.65rem;
  background: linear-gradient(135deg, var(--warning-color), #ffa000);
  color: #000;
  border-radius: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  box-shadow: 0 2px 6px rgba(255, 193, 7, 0.3);
}

.debug-badge {
  font-size: 0.7rem;
  padding: 0.25rem 0.65rem;
  background: linear-gradient(135deg, var(--danger-color), #c82333);
  color: #fff;
  border-radius: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  animation: pulseGlow 2s ease-in-out infinite;
  box-shadow: 0 2px 6px rgba(220, 53, 69, 0.4);
}

@keyframes pulseGlow {
  0%, 100% { 
    opacity: 1;
    box-shadow: 0 2px 6px rgba(220, 53, 69, 0.4);
  }
  50% { 
    opacity: 0.8;
    box-shadow: 0 4px 12px rgba(220, 53, 69, 0.6);
  }
}

.debug-warning {
  margin-top: 0.5rem;
  padding: 1rem;
  background: rgba(255, 193, 7, 0.1);
  border-left: 4px solid var(--warning-color);
  border-radius: 8px;
  color: #ffc107;
  font-weight: 600;
  font-size: 0.9rem;
  line-height: 1.5;
  box-shadow: inset 0 2px 8px rgba(255, 193, 7, 0.1);
}

.field-description {
  font-size: 0.9rem;
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
  background: rgba(51, 161, 253, 0.05);
  border-radius: 8px;
  transition: all 0.2s ease;
}

.checkbox-control:hover {
  background: rgba(51, 161, 253, 0.1);
}

.checkbox-control input[type="checkbox"] {
  width: 1.5rem;
  height: 1.5rem;
  cursor: pointer;
  accent-color: var(--primary-color);
}

.checkbox-label {
  font-weight: 600;
  font-size: 1rem;
  color: var(--text-primary);
}

.slider-control {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.slider-control input[type="range"] {
  height: 8px;
  border-radius: 4px;
  background: linear-gradient(to right, var(--primary-color), var(--teal));
}

.slider-value {
  display: flex;
  justify-content: flex-end;
}

.value-input {
  width: 120px;
  text-align: center;
  font-weight: 600;
  font-size: 1.1rem;
  padding: 0.6rem;
  background: white;
  border: 2px solid var(--border-color);
  transition: all 0.2s ease;
}

.value-input:focus {
  border-color: var(--primary-color);
  background: white;
  box-shadow: 0 0 12px rgba(51, 161, 253, 0.3);
}

.color-control {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  padding: 0.75rem;
  background: rgba(51, 161, 253, 0.05);
  border-radius: 8px;
}

.color-control input[type="color"] {
  width: 80px;
  height: 50px;
  border: 3px solid var(--border-color);
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.color-control input[type="color"]:hover {
  border-color: var(--primary-color);
  box-shadow: 0 4px 12px rgba(51, 161, 253, 0.3);
}

.color-values {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.color-value {
  font-size: 0.9rem;
  color: var(--text-secondary);
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-weight: 500;
}

.select-control select {
  width: 100%;
  padding: 0.75rem;
  background: white;
  border: 2px solid var(--border-color);
  font-size: 1rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.select-control select:hover {
  border-color: var(--primary-color);
  background: white;
}

.select-control select:focus {
  box-shadow: 0 0 12px rgba(51, 161, 253, 0.3);
}

.input-control input {
  width: 100%;
  padding: 0.75rem;
  background: white;
  border: 2px solid var(--border-color);
  font-size: 1rem;
  font-weight: 500;
  transition: all 0.2s ease;
}

.input-control input:hover {
  border-color: var(--primary-color);
  background: white;
}

.input-control input:focus {
  box-shadow: 0 0 12px rgba(51, 161, 253, 0.3);
}
</style>

