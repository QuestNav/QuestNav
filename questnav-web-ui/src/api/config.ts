// API client for configuration endpoints

import { QuestNavApi } from './questnav'
import type { ConfigResponse, ConfigUpdateRequest, SimpleResponse, ServerInfo, HeadsetStatus, AprilTagFieldLayoutEntry } from '../types'

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
  
  async resetPose(): Promise<SimpleResponse> {
    return this.request('/api/reset-pose', { method: 'POST' })
  }

  async getAprilTagFieldLayouts(): Promise<AprilTagFieldLayoutEntry[]> {
    return this.request<AprilTagFieldLayoutEntry[]>('/api/apriltag-field-layouts')
  }

  /**
   * Returns the raw JSON content of a custom (user-uploaded) layout. Bundled layouts
   * are read-only and the server returns 403 for them; the UI should never call this
   * for a bundled entry.
   */
  async getAprilTagFieldLayoutContent(fileName: string): Promise<string> {
    const url = `${this.baseUrl}/api/apriltag-field-layouts/${encodeURIComponent(fileName)}`
    const res = await fetch(url)
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: res.statusText }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return await res.text()
  }

  /**
   * Creates or replaces a custom layout. The same endpoint is used for both create
   * and edit flows; sending an existing name with new content overwrites in place.
   */
  async saveCustomAprilTagFieldLayout(name: string, content: string): Promise<{ success: boolean, fileName: string, tagCount: number }> {
    return this.request('/api/apriltag-field-layouts', {
      method: 'POST',
      body: JSON.stringify({ name, content })
    })
  }

  async renameCustomAprilTagFieldLayout(oldName: string, newName: string): Promise<{ success: boolean, oldFileName: string, newFileName: string }> {
    return this.request(`/api/apriltag-field-layouts/${encodeURIComponent(oldName)}`, {
      method: 'PATCH',
      body: JSON.stringify({ newName })
    })
  }

  async deleteCustomAprilTagFieldLayout(fileName: string): Promise<{ success: boolean, deletedFileName: string, fellBackTo: string | null }> {
    return this.request(`/api/apriltag-field-layouts/${encodeURIComponent(fileName)}`, {
      method: 'DELETE'
    })
  }
}

export const configApi = new ConfigApi()

