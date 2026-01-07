---
title: Passthrough 
---
## Passthrough Video

:::warning
When streaming to the Driver Station during matches, be cautious of bandwidth utilization. Check the current FRC Game Rules, but in past seasons total bandwidth has been limited to 4-7 Mbits/second. Adjust the resolution, compression, and framerate to minimize bandwidth and latency.
:::

QuestNav can stream the video from the Quest headset to a browser for use in a driver station display, recording, additional processing, etc. The stream is available at `http://<Quest IP Address>:5801/video`

### Configuration

Passthrough video requires the "Allow headset camera access" permission in order to work. To verify QuestNav has been granted this permission:

1. Open the Quest main menu
2. Access the Settings menu
3. Choose **Privacy and Safety** from the left-hand side
4. Select **App Permissions**
5. Select **Headset Cameras**
6. Ensure the slider next to QuestNav is <span style={{color:"green",fontWeight:"bold"}}>ENABLED</span>

Passthrough video is _DISABLED_ by default. It can be enabled and configured from the Settings tab of the web interface (eg: `http://10.TE.AM.201:5801`).

- **Passthrough Camera Stream**: Disabled by default.
- **High Quality Passthrough Stream**: Disabled by default. Allows resolutions above 640x480 and framerates faster than 30 fps to be selected in the configuration UI and with dashboards.

:::warning
High quality streaming can put heavy load on the headset CPU. This increased load may lead "dropped frames" for tracking data sent to your robot. High CPU also increases battery drain. High quality video can use excessive bandwidth (up to 100 Mbits/second), which can far exceed the available bandwidth during an FRC match.
:::

### Streaming To Dashboard
Once enabled, Passthrough video can be viewed in your Dashboard (e.g. Elastic)

There is an option to adjust the stream quality in most modern FRC dashboards. QuestNav respects these settings and finds the mode that meets or exceeds the requested values.