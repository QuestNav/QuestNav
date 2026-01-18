import { QuestNavApi } from './questnav'
import type { VideoModeModel } from '../types'

class VideoApi extends QuestNavApi {
  async getVideoModes(): Promise<VideoModeModel[]> {
    return this.request<VideoModeModel[]>('/api/video-modes')
  }
}

export const videoApi = new VideoApi()
