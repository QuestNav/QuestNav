<template>
  <div class="config-field" :class="fieldClass">
    <div class="field-header">
      <label class="field-label">
        {{ title }}
        <slot name="badge"></slot>
      </label>
      <span class="field-description">
        <slot name="description">{{ description }}</slot>
      </span>
    </div>
    <div class="field-control" :class="controlClass">
      <slot></slot>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  description?: string
  fieldClass?: string | object | Array<string | object>
  controlClass?: string | object | Array<string | object>
}>()
</script>

<style scoped>
.config-field {
  padding: 1.5rem;
  background: var(--bg-tertiary);
  border: 1px solid var(--border-color);
  border-radius: 8px;
}

.config-field.field-warning {
  border-color: var(--warning-color);
  background: rgba(255, 193, 7, 0.1);
}

.config-field.field-override-active {
  border-color: var(--warning-color);
  background: rgba(255, 193, 7, 0.15);
}

.config-field.field-override-active .field-description {
  color: var(--warning-color);
}

.field-header {
  margin-bottom: 1rem;
}

.field-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.field-description {
  font-size: 0.875rem;
  color: var(--text-secondary);
}

.input-control {
  display: flex;
  gap: 0.5rem;
}

.input-control :deep(input:not([type="range"])) {
  flex: 1;
  padding: 0.5rem 0.75rem;
  border: 2px solid var(--border-color);
  border-radius: 6px;
  font-size: 1rem;
}

[data-theme="dark"] .input-control :deep(input:not([type="range"])) {
  border-color: #4a5568;
  background: #2d3748;
  color: #e2e8f0;
}

[data-theme="dark"] .input-control :deep(input:not([type="range"])):focus {
  border-color: var(--primary-color);
  background: #374151;
}

.checkbox-control {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.checkbox-control :deep(input) {
  width: 1.25rem;
  height: 1.25rem;
}

.reset-field {
  grid-column: 1 / -1;
}

.database-field {
  grid-column: 1 / -1;
}

.database-buttons {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}
</style>
