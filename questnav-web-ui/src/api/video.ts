import type { VideoModeModel } from '../types'

async function getVideoModes(): Promise<VideoModeModel[]> {
  const response = await fetch('/api/video-modes')
  if (!response.ok) {
    throw new Error('Failed to fetch video modes')
  }
  return response.json()
}

export const videoApi = {
  getVideoModes,
}
