// API client for configuration endpoints

import type { ConfigSchema, ConfigUpdateRequest, ConfigUpdateResponse, ServerInfo, HeadsetStatus } from '../types'

class ConfigApi {
  private baseUrl: string

  constructor() {
    // In development, use proxy; in production, use current host
    this.baseUrl = import.meta.env.DEV ? '' : window.location.origin
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...options.headers as Record<string, string>
    }

    // Create timeout controller
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), 5000) // 5 second timeout

    try {
      const response = await fetch(`${this.baseUrl}${endpoint}`, {
        ...options,
        headers,
        signal: controller.signal
      })

      clearTimeout(timeoutId)

      if (!response.ok) {
        const error = await response.json().catch(() => ({ message: response.statusText }))
        throw new Error(error.message || `HTTP ${response.status}`)
      }

      return response.json()
    } catch (error) {
      clearTimeout(timeoutId)
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timeout - headset may be disconnected')
      }
      throw error
    }
  }

  async getSchema(): Promise<ConfigSchema> {
    return this.request<ConfigSchema>('/api/schema')
  }

  async getConfig(): Promise<{ success: boolean; values: Record<string, any>; timestamp: number }> {
    return this.request('/api/config')
  }

  async updateConfig(path: string, value: any): Promise<ConfigUpdateResponse> {
    return this.request<ConfigUpdateResponse>('/api/config', {
      method: 'POST',
      body: JSON.stringify({ path, value } as ConfigUpdateRequest)
    })
  }

  async getServerInfo(): Promise<ServerInfo> {
    return this.request<ServerInfo>('/api/info')
  }

  async getHeadsetStatus(): Promise<HeadsetStatus> {
    return this.request<HeadsetStatus>('/api/status')
  }
  
  async getLogs(count: number = 100): Promise<{ success: boolean, logs: any[] }> {
    return this.request(`/api/logs?count=${count}`)
  }
  
  async clearLogs(): Promise<{ success: boolean, message: string }> {
    return this.request('/api/logs', { method: 'DELETE' })
  }
  
  async restartApp(): Promise<{ success: boolean, message: string }> {
    return this.request('/api/restart', { method: 'POST' })
  }
  
  async resetPose(): Promise<{ success: boolean, message: string }> {
    return this.request('/api/reset-pose', { method: 'POST' })
  }

}


export const configApi = new ConfigApi()

