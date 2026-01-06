# QuestNav ConfigServer Web API

This document describes the HTTP endpoints exposed by ConfigServer for QuestNav. It includes the path to each endpoint, supported methods, parameters, request/response formats, and response codes.

# Overview
- Base URL: `http://<device-ip>:<port>/`
  - Default port is 5801 and is configurable via WebServerConstants.serverPort.
- Content Type: application/json for all /api endpoints, unless otherwise noted.
- CORS: When WebServerConstants.enableCORSDevMode is true, the server sets `Access-Control-Allow-Origin: *` and supports preflight with `OPTIONS` (responds 200).
- Errors: Unhandled exceptions return HTTP 500 with a JSON body `{ "success": false, "message": "..." }`.

## Common Models
- SimpleResponse
  - success: `boolean`
  - message: `string`

# Endpoints

## GET /api/config
- Description: Returns current configuration values.
- Request: none
- Responses:
  - 200 OK → `ConfigValuesResponse`
    - success: `boolean`
    - timestamp: `number` (Unix seconds)
    - teamNumber: `number`
    - debugIpOverride: `string` (empty string if not set)
    - enableAutoStartOnBoot: `boolean`
    - enablePassthroughStream: `boolean`
    - enableDebugLogging: `boolean`
```json
{
  "success": true,
  "teamNumber": 9999,
  "debugIpOverride": "",
  "enableAutoStartOnBoot": true,
  "enablePassthroughStream": false,
  "enableDebugLogging": false,
  "timestamp": 1765759907
}
```
- Errors:
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Failed to load config" }
```

## POST /api/config
- Description: Updates a single configuration value and persists it.
- Request:
  - Headers: `Content-Type: application/json`
  - Body → `ConfigUpdateRequest`
    - teamNumber: `number` (optional)
    - debugIpOverride: `string` (empty string if not set)
    - enableAutoStartOnBoot: `boolean` (optional)
    - enablePassthroughStream: `boolean` (optional)
    - enableDebugLogging: `boolean` (optional)
- Request example:
```json
{ "path": "WebServerConstants/webConfigTeamNumber", "value": 1234 }
```
- Responses:
  - 200 OK → `ConfigUpdateResponse` (on success)
    - success: `boolean`
    - message: `string`
    - oldValue: `any`
    - newValue: `any`
```json
{ "success": true, "message": "Configuration updated" }
```
  - 400 Bad Request → SimpleResponse
```json
{ "success": false, "message": "Invalid request" }
```
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## POST /api/reset-config
- Description: Resets all configuration values to their defaults and persists them.
- Request: none (no body)
- Responses:
- 200 OK → SimpleResponse
```json
{ "success": true, "message": "Configuration reset to defaults" }
```
- 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## GET /api/download-database
- Description: Downloads the entire SQLite configuration atabase.
- Request: non
- Responses:
- 200 OK → `application/octet-stream` containing the SQLite database file.

## POST /api/upload-database
- Description: Uploads a new SQLite configuration database file.
- Request: Body containing the SQLite database file.
- Responses:
- 200 OK → SimpleResponse
```json
{ "success": true, "message": "Database uploaded. Restart app to apply changes." }
```
- 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "No file data received" }
```

## GET /api/info
- Description: Returns application and environment information.
- Request: none
- Responses:
  - 200 OK → `SystemInfoResponse`
    - appName: `string`
    - version: `string`
    - unityVersion: `string`
    - buildDate: `string` (yyyy-MM-dd HH:mm:ss)
    - platform: `string`
    - deviceModel: `string`
    - operatingSystem: `string`
    - connectedClients: `number`
    - configPath: `string`
    - serverPort: `number`
    - timestamp: `number` (Unix seconds)
```json
{
  "appName": "QuestNav",
  "version": "1b1a5c0-dev",
  "unityVersion": "6000.2.7f2",
  "buildDate": "2025-12-05 23:32:54",
  "platform": "Android",
  "deviceModel": "Oculus Quest",
  "operatingSystem": "Android OS 14 / API-34 (UP1A.231005.007.A1/1871560095600610)",
  "connectedClients": 1,
  "configPath": "/storage/emulated/0/Android/data/gg.QuestNav.QuestNav/files/config.json",
  "serverPort": 5801,
  "timestamp": 1765372725
}
```
- Errors:
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## GET /api/status
- Description: Returns current runtime status from StatusProvider.
- Request: none
- Responses:
  - 200 OK → `Status`
    - position: `{ x: number, y: number, z: number }`
    - rotation: `{ x: number, y: number, z: number, w: number }` (quaternion)
    - eulerAngles: `{ pitch: number, yaw: number, roll: number }`
    - isTracking: `boolean`
    - trackingLostEvents: `number`
    - batteryPercent: `number` (0–100)
    - batteryLevel: `number` (0.0–1.0)
    - batteryStatus: `string` (e.g., Charging, Discharging)
    - batteryCharging: `boolean`
    - networkConnected: `boolean`
    - ipAddress: `string`
    - teamNumber: `number`
    - robotIpAddress: `string`
    - fps: `number`
    - frameCount: `number`
    - connectedClients: `number`
    - timestamp: `number` (Unix seconds)
```json
{
  "position": {
    "x": 1.45210493,
    "y": 0.9311298,
    "z": 1.32042563
  },
  "rotation": {
    "x": 0.069944,
    "y": 0.101098642,
    "z": -0.5767848,
    "w": -0.8075928
  },
  "eulerAngles": {
    "pitch": 71.04313,
    "yaw": 345.878448,
    "roll": 0.2092318
  },
  "isTracking": true,
  "trackingLostEvents": 0,
  "batteryPercent": 14,
  "batteryLevel": 0.14,
  "batteryStatus": "Charging",
  "batteryCharging": true,
  "networkConnected": false,
  "ipAddress": "192.168.0.195",
  "teamNumber": 9999,
  "robotIpAddress": "192.168.0.130",
  "fps": 112.0,
  "frameCount": 67927,
  "connectedClients": 1,
  "timestamp": 1765372907
}
```
- Errors:
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## GET /api/logs
- Description: Returns recent Unity log entries (chronological, oldest first).
- Request:
  - Query parameters:
    - count: `number` (optional, default 100) – maximum number of logs
- Request examples:
```
GET /api/logs HTTP/1.1
Host: <device-ip>:5801
```
```
GET /api/logs?count=200 HTTP/1.1
Host: <device-ip>:5801
```
- Responses:
  - 200 OK → `LogsResponse`
    - success: `boolean`
    - logs: `LogEntry[]`
      - LogEntry
        - message: `string`
        - stackTrace: `string`
        - type: `string` (Log | Warning | Error | Assert | Exception)
        - timestamp: `number` (Unix ms)
```json
{
  "success": true,
  "logs": [
    {
      "message": "[WebServerManager] Server started successfully",
      "stackTrace": "UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)\nQuestNav.WebServer.<StartServerCoroutine>d__25:MoveNext()\nUnityEngine.SetupCoroutine:InvokeMoveNext(IEnumerator, IntPtr)\n",
      "type": "Log",
      "timestamp": 1765372337970
    },
    {
      "message": "[OVRManager] TrackingAcquired event",
      "stackTrace": "UnityEngine.DebugLogHandler:Internal_Log(LogType, LogOption, String, Object)\nOVRManager:Update()\n",
      "type": "Log",
      "timestamp": 1765372369208
    }
  ]
}
``` 
- Errors:
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## DELETE /api/logs
- Description: Clears all collected logs.
- Request: none
- Responses:
  - 200 OK → `SimpleResponse`
```json
{ "success": true, "message": "Logs cleared" }
```
- Errors:
  - 500 Internal Server Error → `SimpleResponse`
```json
{ "success": false, "message": "Unexpected error" }
```

## POST /api/restart
- Description: Triggers application restart (asynchronously).
- Request: none (no body)
- Responses:
  - 200 OK → `SimpleResponse`
```json
{ "success": true, "message": "Restart initiated" }
```
- Notes: The restart is triggered after the response is sent.
- Errors:
  - 500 Internal Server Error → SimpleResponse
```json
{ "success": false, "message": "Unexpected error" }
```

## POST /api/reset-pose
- Description: Triggers VR pose reset on the device.
- Request: none (no body)
- Response: 200 OK → `SimpleResponse`
```json
{ "success": true, "message": "Pose reset initiated" }
```
- Errors:
  - 500 Internal Server Error → `SimpleResponse`
```json
{ "success": false, "message": "Unexpected error" }
```

## GET /api/video-modes
- Description: Gets the available passthrough stream capture video modes
- Request: none (no body)
- Response: 200 OK → VideoModeModel[]
```json
[
  {"width": 100, "height": 100, "framerate": 24},
  {"width": 200, "height": 200, "framerate": 24},
]
```
- Errors:
  - 503 Service Unavailable → `SimpleResponse`
```json
{ "success": false, "message": "Passthrough stream not available" }
```
```json
{ "success": false, "message": "Stream not initialized. Enable passthrough stream first." }
```

## GET /video
- Description: MJPEG video stream endpoint (multipart/x-mixed-replace) for passthrough camera or other frame sources.
- Request: none
- Responses:
  - 200 OK when streaming is available.
    - Headers: `Content-Type: multipart/x-mixed-replace; boundary=--frame`
    - Body: sequence of JPEG frames separated by boundary `--frame` with per-frame headers including `Content-Type: image/jpeg` and `Content-Length`.
    - Sample (truncated):
```
--frame
Content-Type: image/jpeg
Content-Length: 12345

<binary jpg bytes>
--frame
Content-Type: image/jpeg
Content-Length: 12001

<binary jpg bytes>
--frame
```
  - 204 No Content → if the stream provider is not initialized in ConfigServer (text/plain message: "streamProvider is not initialized").
  - 503 Service Unavailable → if the VideoStreamProvider has no frame source configured (text/plain message: "The stream is unavailable").
- Notes: This endpoint is not JSON.

Other behaviors
- Preflight: Any /api endpoint accepts `OPTIONS` and returns 200 with CORS headers when enabled.
- Unknown routes under /api: 404 Not Found with JSON body `{ "success": false, "message": "Not found" }`.

Examples
- Get schema:
```
  curl -s http://<device-ip>:5801/api/schema
```

- Get config values:
```
  curl -s http://<device-ip>:5801/api/config
```

- Update config value:
```
  curl -s -X POST http://<device-ip>:5801/api/config \
       -H "Content-Type: application/json" \
       -d '{"path":"WebServerConstants/webConfigTeamNumber","value":1234}'
```

- Fetch logs (last 200):
```
  curl -s "http://<device-ip>:5801/api/logs?count=200"
```

- Clear logs:
```
  curl -s -X DELETE http://<device-ip>:5801/api/logs
```

- Restart app:
```
  curl -s -X POST http://<device-ip>:5801/api/restart
```

- Reset pose:
```
  curl -s -X POST http://<device-ip>:5801/api/reset-pose
```

- View MJPEG stream (in a browser):
  `http://<device-ip>:5801/video`
