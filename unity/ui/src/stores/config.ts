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
      // Don't set error if it's just a network error (server suspended/offline)
      if (err instanceof Error && err.message.includes('Failed to fetch')) {
        // Silent fail - connection status indicator will show it's disconnected
        error.value = null
      } else {
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
      // Don't set error if it's just a network error (server suspended/offline)
      if (err instanceof Error && err.message.includes('Failed to fetch')) {
        // Silent fail - connection status indicator will show it's disconnected
        error.value = null
      } else {
        error.value = err instanceof Error ? err.message : 'Failed to load config'
      }
      return false
    }
  }

  async function updateValue(path: string, value: any) {
    try {
      const response = await configApi.updateConfig(path, value)
      
      if (response.success) {
        values.value[path] = response.newValue
        lastUpdated.value = Date.now()
      }
      
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update value'
      return false
    }
  }

  return {
    // State
    schema,
    values,
    isLoading,
    error,
    lastUpdated,
    
    // Computed
    categories,
    fieldsByCategory,
    
    // Actions
    loadSchema,
    loadConfig,
    updateValue
  }
})

