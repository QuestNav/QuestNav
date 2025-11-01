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

    const response = await fetch(`${this.baseUrl}${endpoint}`, {
      ...options,
      headers
    })

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }))
      throw new Error(error.message || `HTTP ${response.status}`)
    }

    return response.json()
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

}


export const configApi = new ConfigApi()

