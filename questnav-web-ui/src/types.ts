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
  // Blacklist: detections with one of these IDs are dropped before the PoseLib solver runs.
  // Empty array means detect every tag. UI clamps entries to [0, 50] (FRC tag range).
  ignoredIds: number[]
  maxDistance: number
  minimumNumberOfTags: number
  // Field-layout JSON file used at app startup. Restart-on-change: writing this value
  // persists it but the running app keeps using the previously-loaded layout. The UI
  // surfaces the restart-required state with an inline banner + restart button.
  fieldLayoutFile: string
}

export interface AprilTagFieldLayoutEntry {
  fileName: string
  displayName: string
  source: 'bundled' | 'custom'
  tagCount: number
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

  // Camera arbitration: AprilTag detector wins over the passthrough stream when
  // both are enabled. The passthrough video keeps showing whatever frames the
  // camera is producing, but the resolution dropdown is locked.
  passthroughResolutionLockedByAprilTag: boolean
  effectivePassthroughResolution: { width: number, height: number } | null

  timestamp: number
}

