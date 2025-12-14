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

## GET /api/schema
- Description: Returns the full configuration schema derived from [Config] attributes.
- Request: none
- Response: 200 OK
  - JSON object with fields and categories:
    - fields: `ConfigFieldSchema[]`
      - path: `string`
      - displayName: `string`
      - description: `string`
      - category: `string`
      - type: `string` (e.g., int, float, bool, string, color)
      - controlType: `string` (e.g., slider, input, checkbox, select, color)
      - min, max, step: `number` | null
      - defaultValue: `any`
      - currentValue: `any`
      - requiresRestart: `boolean`
      - order: `number`
      - options: `string`[] | null
    - categories: `{ category(string): ConfigFieldSchema[] }`
    - version: `string`
```json
{
  "fields": [
    {
      "path": "WebServerConstants/webConfigTeamNumber",
      "displayName": "Team Number",
      "description": "FRC team number for NetworkTables connection (1-25599)",
      "category": "QuestNav",
      "type": "int",
      "controlType": "input",
      "min": 1,
      "max": 25599,
      "step": null,
      "defaultValue": 0,
      "currentValue": 9999,
      "requiresRestart": false,
      "order": 1,
      "options": null
    }
  ],
  "categories": {
    "QuestNav": [
      {
        "path": "WebServerConstants/webConfigTeamNumber",
        "displayName": "Team Number",
        "description": "FRC team number for NetworkTables connection (1-25599)",
        "category": "QuestNav",
        "type": "int",
        "controlType": "input",
        "min": 1,
        "max": 25599,
        "step": null,
        "defaultValue": 0,
        "currentValue": 9999,
        "requiresRestart": false,
        "order": 1,
        "options": null
      }
    ]
  },
  "version": "1.0"
}
```
- Errors: 500 Internal Server Error → SimpleResponse

## GET /api/config
- Description: Returns current configuration values.
- Request: none
- Response: 200 OK → `ConfigValuesResponse`
  - success: `boolean`
  - values: `{ [path: string]: any }`
  - timestamp: `number` (Unix seconds)
```json
{
  "success": true,
  "values": {
    "WebServerConstants/webConfigTeamNumber": 9999,
    "WebServerConstants/debugNTServerAddressOverride": "192.168.0.130",
    "WebServerConstants/ntServerPort": 5810,
    "WebServerConstants/mainUpdateHz": 100,
    "WebServerConstants/slowUpdateHz": 3,
    "WebServerConstants/displayFrequency": 120.0,
    "WebServerConstants/autoStartOnBoot": false,
    "WebServerConstants/poseResetTtlMs": 50,
    "WebServerConstants/maxPoseReadRetries": 3,
    "WebServerConstants/positionErrorThreshold": 0.01,
    "WebServerConstants/ntLogLevelMin": 9,
    "WebServerConstants/enablePassThrough": true,
    "WebServerConstants/passthroughVideo": "./video",
    "WebServerConstants/serverPort": 5801,
    "WebServerConstants/enableCORSDevMode": false,
    "WebServerConstants/enableDebugLogging": false
  },
  "timestamp": 1765372400
}
```
- Errors: 500 Internal Server Error → SimpleResponse

## POST /api/config
- Description: Updates a single configuration value and persists it.
- Headers: Content-Type: application/json
- Body: `ConfigUpdateRequest`
  - path: `string` (e.g., "WebServerConstants/webConfigTeamNumber")
  - value: `any` (converted to appropriate type)
```json
{ "path": "WebServerConstants/webConfigTeamNumber", "value": 1234 }
```
- Responses:
  - 200 OK → `ConfigUpdateResponse` (on success)
    - success: `boolean`
    - message: `string`
    - oldValue: `any`
    - newValue: `any`
  - 400 Bad Request →
    - If request is missing/invalid: SimpleResponse `{ success:false, message:"Invalid request" }`
    - If update fails: ConfigUpdateResponse `{ success:false, message:"Failed to update configuration" }`
  - 500 Internal Server Error → SimpleResponse

## GET /api/info
- Description: Returns application and environment information.
- Request: none
- Response: 200 OK → `SystemInfoResponse`
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
- Errors: 500 Internal Server Error → SimpleResponse

## GET /api/status
- Description: Returns current runtime status from StatusProvider.
- Request: none
- Response: 200 OK → `Status`
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
- Errors: 500 Internal Server Error → SimpleResponse

## GET /api/logs
- Description: Returns recent Unity log entries (chronological, oldest first).
- Query Parameters:
  - count: `number` (optional, default 100) – maximum number of logs
- Response: 200 OK → `LogsResponse`
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
- Errors: 500 Internal Server Error → SimpleResponse

## DELETE /api/logs
- Description: Clears all collected logs.
- Request: none
- Response: 200 OK → `SimpleResponse` `{ success:true, message:"Logs cleared" }`
- Errors: 500 Internal Server Error → `SimpleResponse`

## POST /api/restart
- Description: Triggers application restart (asynchronously).
- Request: none (no body)
- Response: 200 OK → `SimpleResponse` `{ success:true, message:"Restart initiated" }`
- Notes: The restart is triggered after the response is sent.
- Errors: 500 Internal Server Error → SimpleResponse

## POST /api/reset-pose
- Description: Triggers VR pose reset on the device.
- Request: none (no body)
- Response: 200 OK → `SimpleResponse` `{ success:true, message:"Pose reset initiated" }`
- Errors: 500 Internal Server Error → `SimpleResponse`

## GET /video
- Description: MJPEG video stream endpoint (multipart/x-mixed-replace) for passthrough camera or other frame sources.
- Request: none
- Responses:
  - 200 OK when streaming is available.
    - Headers: `Content-Type: multipart/x-mixed-replace; boundary=--frame`
    - Body: sequence of JPEG frames separated by boundary `--frame` with per-frame headers including `Content-Type: image/jpeg` and `Content-Length`.
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
