<template>
  <div class="config-field">
    <div class="field-header">
      <label :for="field.path" class="field-label">
        {{ field.displayName }}
        <span v-if="field.requiresRestart" class="restart-badge">Requires Restart</span>
      </label>
      <span v-if="field.description" class="field-description">
        {{ field.description }}
      </span>
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
import { ref, watch } from 'vue'
import type { ConfigFieldSchema } from '../types'

const props = defineProps<{
  field: ConfigFieldSchema
  value: any
}>()

const emit = defineEmits<{
  update: [path: string, value: any]
}>()

const localValue = ref(props.value)

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
  gap: 0.75rem;
}

.field-header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.field-label {
  font-weight: 600;
  color: var(--text-primary);
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.restart-badge {
  font-size: 0.75rem;
  padding: 0.125rem 0.5rem;
  background-color: var(--warning-color);
  color: #000;
  border-radius: 12px;
  font-weight: 500;
}

.field-description {
  font-size: 0.875rem;
  color: var(--text-muted);
  line-height: 1.4;
}

.field-control {
  width: 100%;
}

.checkbox-control {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.checkbox-label {
  font-weight: 500;
  color: var(--text-secondary);
}

.slider-control {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.slider-value {
  display: flex;
  justify-content: flex-end;
}

.value-input {
  width: 100px;
  text-align: center;
}

.color-control {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.color-values {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.color-value {
  font-size: 0.875rem;
  color: var(--text-secondary);
  font-family: monospace;
}

.select-control select,
.input-control input {
  width: 100%;
}
</style>

