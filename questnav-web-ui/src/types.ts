// Type definitions for configuration system

export interface ConfigFieldSchema {
  path: string
  displayName: string
  description: string
  category: string
  type: 'int' | 'float' | 'double' | 'bool' | 'string' | 'color'
  controlType: 'slider' | 'input' | 'checkbox' | 'select' | 'color'
  min?: number
  max?: number
  step?: number
  defaultValue: any
  currentValue: any
  requiresRestart: boolean
  order: number
  options?: string[]
}

export interface ConfigSchema {
  fields: ConfigFieldSchema[]
  categories: Record<string, ConfigFieldSchema[]>
  version: string
}

export interface ConfigUpdateRequest {
  path: string
  value: any
}

export interface ConfigUpdateResponse {
  success: boolean
  message: string
  oldValue?: any
  newValue?: any
}

export interface ServerInfo {
  // App Information
  appName: string
  version: string
  unityVersion: string
  buildDate: string
  
  // Platform Information
  platform: string
  deviceModel: string
  deviceName: string
  operatingSystem: string
  
  // System Information
  processorType: string
  processorCount: number
  systemMemorySize: number
  graphicsDeviceName: string
  
  // Config Information
  connectedClients: number
  configPath: string
  serverPort: number
  
  timestamp: number
}

export interface HeadsetStatus {
  // Pose
  position: { x: number, y: number, z: number }
  rotation: { x: number, y: number, z: number, w: number }
  eulerAngles: { pitch: number, yaw: number, roll: number }
  
  // Tracking
  isTracking: boolean
  trackingLostEvents: number
  
  // Battery
  batteryPercent: number
  batteryLevel: number
  batteryStatus: string
  batteryCharging: boolean
  
  // Network
  networkConnected: boolean
  ipAddress: string
  teamNumber: number
  robotIpAddress: string
  
  // Performance
  fps: number
  frameCount: number
  
  // Web Interface
  connectedClients: number
  
  timestamp: number
}

