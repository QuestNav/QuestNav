// API client for configuration endpoints

import { QuestNavApi } from './questnav'
import type { ConfigResponse, ConfigUpdateRequest, SimpleResponse, ServerInfo, HeadsetStatus } from '../types'

class ConfigApi extends QuestNavApi {

  async getConfig(): Promise<ConfigResponse> {
    return this.request<ConfigResponse>('/api/config')
  }

  async updateConfig(update: ConfigUpdateRequest): Promise<SimpleResponse> {
    return this.request<SimpleResponse>('/api/config', {
      method: 'POST',
      body: JSON.stringify(update)
    })
  }

  async resetConfig(): Promise<SimpleResponse> {
    return this.request<SimpleResponse>('/api/reset-config', { method: 'POST' })
  }

  async downloadDatabase(): Promise<void> {
    const response = await fetch(`${this.baseUrl}/api/download-database`)
    if (!response.ok) {
      throw new Error('Failed to download database')
    }
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = 'config.db'
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  }

  async uploadDatabase(file: File): Promise<SimpleResponse> {
    const response = await fetch(`${this.baseUrl}/api/upload-database`, {
      method: 'POST',
      body: file
    })
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: response.statusText }))
      throw new Error(error.message || `HTTP ${response.status}`)
    }
    return response.json()
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
  
  async clearLogs(): Promise<SimpleResponse> {
    return this.request('/api/logs', { method: 'DELETE' })
  }
  
  async restartApp(): Promise<SimpleResponse> {
    return this.request('/api/restart', { method: 'POST' })
  }

  async resetPose(position?: {x: number, y: number, z: number}, eulerAngles?: {pitch: number, roll: number, yaw: number}): Promise<SimpleResponse> {
    const body = {position, eulerAngles}
    return this.request('/api/reset-pose', { method: 'POST', body: JSON.stringify(body) })
  }
}

export const configApi = new ConfigApi()

