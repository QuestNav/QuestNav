import { QuestNavApi } from './questnav'
import type { VideoModeModel } from '../types'

class VideoApi extends QuestNavApi {
  async getVideoModes(): Promise<VideoModeModel[]> {
    return this.request<VideoModeModel[]>('/api/video-modes')
  }

  /**
   * Returns the resolution x framerate cross-product the AprilTag detector can use.
   * Same shape as getVideoModes() but the AprilTag list does not gate on
   * EnableHighQualityStreams - the detector benefits from higher resolution.
   */
  async getAprilTagVideoModes(): Promise<VideoModeModel[]> {
    return this.request<VideoModeModel[]>('/api/apriltag-video-modes')
  }
}

export const videoApi = new VideoApi()
