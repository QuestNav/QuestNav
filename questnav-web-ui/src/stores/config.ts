// Pinia store for configuration state management

import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { ConfigResponse } from '../types'
import { configApi } from '../api/config'

export const useConfigStore = defineStore('config', () => {
  // State
  const config = ref<ConfigResponse | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const lastUpdated = ref<number>(0)

  // Actions
  async function loadConfig(showLoading = true) {
    // Only show loading spinner on initial load, not during polling
    if (showLoading && config.value === null) {
      isLoading.value = true
    }
    error.value = null
    
    try {
      const data = await configApi.getConfig()
      config.value = data
      lastUpdated.value = Date.now()
      return true
    } catch (err) {
      if (err instanceof Error && err.message.includes('Failed to fetch')) {
        error.value = null
      } else {
        error.value = err instanceof Error ? err.message : 'Failed to load config'
      }
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function updateTeamNumber(value: number) {
    try {
      const response = await configApi.updateConfig({ teamNumber: value })
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update'
      return false
    }
  }

  async function updateDebugIpOverride(value: string) {
    try {
      const response = await configApi.updateConfig({ debugIpOverride: value })
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update'
      return false
    }
  }

  async function updateEnableAutoStartOnBoot(value: boolean) {
    try {
      const response = await configApi.updateConfig({ enableAutoStartOnBoot: value })
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update'
      return false
    }
  }

  async function updateEnableDebugLogging(value: boolean) {
    try {
      const response = await configApi.updateConfig({ enableDebugLogging: value })
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update'
      return false
    }
  }

  async function updateEnablePassthroughStream(value: boolean) {
    try {
      const response = await configApi.updateConfig({ enablePassthroughStream: value })
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update'
      return false
    }
  }

  async function resetToDefaults() {
    try {
      const response = await configApi.resetConfig()
      if (response.success) {
        await loadConfig(false)
      }
      return response.success
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to reset'
      return false
    }
  }

  return {
    // State
    config,
    isLoading,
    error,
    lastUpdated,
    
    // Actions
    loadConfig,
    updateTeamNumber,
    updateDebugIpOverride,
    updateEnableAutoStartOnBoot,
    updateEnablePassthroughStream,
    updateEnableDebugLogging,
    resetToDefaults
  }
})

