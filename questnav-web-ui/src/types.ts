// Type definitions for configuration system

export interface StreamModeModel {
  width: number
  height: number
  framerate: number
  quality: number
}

export interface VideoModeModel {
  width: number
  height: number
  framerate: number
}

export interface AprilTagDetectorMode {
  mode: number
  width: number
  height: number
  framerate: number
  allowedIds: number[]
  maxDistance: number
  minimumNumberOfTags: number
}

export interface ConfigResponse {
  success: boolean
  teamNumber: number
  debugIpOverride: string
  enableAutoStartOnBoot: boolean
  enablePassthroughStream: boolean
  enableHighQualityStream: boolean
  enableDebugLogging: boolean
  streamMode: StreamModeModel
  enableAprilTagDetector: boolean
  aprilTagDetectorMode: AprilTagDetectorMode
  timestamp: number
}

export interface ConfigUpdateRequest {
  teamNumber?: number
  debugIpOverride?: string
  enableAutoStartOnBoot?: boolean
  enablePassthroughStream?: boolean
  enableHighQualityStream?: boolean
  enableDebugLogging?: boolean
  streamMode?: StreamModeModel
  enableAprilTagDetector?: boolean
  aprilTagDetectorMode?: AprilTagDetectorMode
}

export interface SimpleResponse {
  success: boolean
  message: string
}

export interface ServerInfo {
  appName: string
  version: string
  unityVersion: string
  buildDate: string
  platform: string
  deviceModel: string
  operatingSystem: string
  connectedClients: number
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

