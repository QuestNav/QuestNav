---
title: Passthrough 
---
## Passthrough Video

QuestNav can stream the video from the Quest headset to a browser for use in a driver station display, recording, additional processing, etc. The stream is available at `http://<Quest IP Address>:5801/video`

### Configuration

Passthrough video requires the "Allow headset camera access" permission in order to work. To verify QuestNav has been granted this permission:

1. Open the Quest main menu
2. Access the Settings menu
3. Choose **Privacy and Safety** from the left-hand side
4. Select **App Permissions**
5. Select **Headset Cameras**
6. Ensure the slider next to QuestNav is <span style={{color:"green",fontWeight:"bold"}}>ENABLED</span>

Passthrough video is _DISABLED_ by default. It can be enabled and configured from the Passthrough tab of the web interface (eg: `http://10.TE.AM.201:5801`).

- **Enable Passthrough Video**: Disabled by default.
- **Video Frame Rate**: (default: 15) Controls the maximum frame rate of the streamed video.

