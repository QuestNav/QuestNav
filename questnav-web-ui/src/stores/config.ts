// Pinia store for configuration state management

import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { ConfigSchema } from '../types'
import { configApi } from '../api/config'

export const useConfigStore = defineStore('config', () => {
  // State
  const schema = ref<ConfigSchema | null>(null)
  const values = ref<Record<string, any>>({})
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const lastUpdated = ref<number>(0)
  const restartRequired = ref(false)

  // Computed
  const categories = computed(() => {
    if (!schema.value) return []
    return Object.keys(schema.value.categories).sort()
  })

  const fieldsByCategory = computed(() => {
    if (!schema.value) return {}
    return schema.value.categories
  })

  // Actions
  async function loadSchema() {
    isLoading.value = true
    error.value = null
    
    try {
      const data = await configApi.getSchema()
      schema.value = data
      
      // Initialize values from schema
      data.fields.forEach(field => {
        values.value[field.path] = field.currentValue
      })
      
      lastUpdated.value = Date.now()
      return true
    } catch (err) {
      // Only show error on initial load, not on reconnection attempts
      if (!schema.value) {
        error.value = err instanceof Error ? err.message : 'Failed to load schema'
      }
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function loadConfig() {
    error.value = null
    
    try {
      const data = await configApi.getConfig()
      values.value = { ...values.value, ...data.values }
      lastUpdated.value = Date.now()
      return true
    } catch (err) {
      // Silently fail - don't set error to keep old data visible
      // Connection state overlay will handle showing disconnect status
      return false
    }
  }

  async function updateValue(path: string, value: any) {
    try {
      const response = await configApi.updateConfig(path, value)
      
      if (response.success) {
        values.value[path] = response.newValue
        lastUpdated.value = Date.now()
        
        // Check if this field requires restart
        const field = schema.value?.fields.find(f => f.path === path)
        if (field?.requiresRestart) {
          restartRequired.value = true
        }
      }
      
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update value'
      return false
    }
  }
  
  function clearRestartFlag() {
    restartRequired.value = false
  }

  return {
    // State
    schema,
    values,
    isLoading,
    error,
    lastUpdated,
    restartRequired,
    
    // Computed
    categories,
    fieldsByCategory,
    
    // Actions
    loadSchema,
    loadConfig,
    updateValue,
    clearRestartFlag
  }
})

