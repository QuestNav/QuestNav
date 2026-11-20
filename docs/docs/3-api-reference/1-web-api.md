# Web API

The QuestNav app runs a small HTTP server on the headset. It serves the web interface and the JSON API that the
interface uses. This page lists every route the server handles (`unity/Assets/QuestNav/WebServer/Server/ConfigServer.cs`).

## Basics

| | |
|---|---|
| Base URL | `http://<quest-ip>:5801/` |
| Web interface | `/` (static files) |
| JSON API | `/api/...` |
| Camera stream | `/video` |

The server starts after the tracking loop is running, so it can take a few seconds after the app opens before it
answers.

### Common rules for `/api`

- **Response format.** Responses are JSON. Errors use a `SimpleResponse`:
  ```json
  { "success": false, "message": "..." }
  ```
- **Same origin only.** The server sends no CORS headers. A `POST`, `PUT`, `PATCH` or `DELETE` whose `Origin`
  header is present and does not match its `Host` header gets **403** with the message
  `Cross-origin requests are not allowed.` An `Origin` of `null` is also rejected. Requests without an `Origin`
  header, such as curl or scripts, are allowed. `GET` requests are not checked.
- **`OPTIONS`.** Any `OPTIONS /api/...` request returns **204** with
  `Allow: GET, POST, PATCH, DELETE, OPTIONS`.
- **JSON bodies.** These must be sent with `Content-Type: application/json`; parameters such as `charset` are
  ignored. Any other type gets **415** `Content-Type must be application/json.`
- **Body size limits.** A body over the limit gets **413** `Request body too large (max ...).`

  | Request | Limit |
  |---|---|
  | JSON requests | 64 KB |
  | Field-layout uploads | 1 MB |
  | Database uploads | 4 MB |

- **Unknown routes.** An unknown route or verb gets **404** `Not found`.
- **Unhandled errors.** These return **500** with the exception message.

### Endpoint summary

| Method | Path | Purpose |
|---|---|---|
| GET | [`/api/config`](#get-apiconfig) | Read the configuration |
| POST | [`/api/config`](#post-apiconfig) | Change configuration values |
| POST | [`/api/reset-config`](#post-apireset-config) | Reset the configuration to defaults |
| GET | [`/api/download-database`](#get-apidownload-database) | Download the configuration database |
| POST | [`/api/upload-database`](#post-apiupload-database) | Replace the configuration database |
| GET | [`/api/info`](#get-apiinfo) | App and device information |
| GET | [`/api/status`](#get-apistatus) | Live tracking, battery and network status |
| GET | [`/api/logs`](#get-apilogs) | Recent log messages |
| DELETE | [`/api/logs`](#delete-apilogs) | Clear the log buffer |
| POST | [`/api/restart`](#post-apirestart) | Restart the app |
| POST | [`/api/reset-pose`](#post-apireset-pose) | Reset the pose to the field origin |
| GET | [`/api/video-modes`](#get-apivideo-modes) | Camera modes for the passthrough stream |
| GET | [`/api/apriltag-video-modes`](#get-apiapriltag-video-modes) | Camera modes the AprilTag detector accepts |
| GET | [`/api/apriltag-field-layouts`](#get-apiapriltag-field-layouts) | List field layouts |
| POST | [`/api/apriltag-field-layouts`](#post-apiapriltag-field-layouts) | Upload or replace a custom layout |
| GET | [`/api/apriltag-field-layouts/{name}`](#get-apiapriltag-field-layoutsname) | Download a custom layout |
| PATCH | [`/api/apriltag-field-layouts/{name}`](#patch-apiapriltag-field-layoutsname) | Rename a custom layout |
| DELETE | [`/api/apriltag-field-layouts/{name}`](#delete-apiapriltag-field-layoutsname) | Delete a custom layout |
| GET | [`/video`](#get-video) | MJPEG passthrough camera stream |

## Configuration

### GET /api/config

Returns the current configuration.

```json
{
  "success": true,
  "teamNumber": 9999,
  "debugIpOverride": "",
  "allowedPoseResetTimeoutMs": 120,
  "enableAutoStartOnBoot": true,
  "enablePassthroughStream": false,
  "enableHighQualityStream": false,
  "streamMode": { "width": 320, "height": 240, "framerate": 24, "quality": 75 },
  "enableAprilTagDetector": true,
  "aprilTagDetectorMode": {
    "mode": 0,
    "width": 1280,
    "height": 1280,
    "framerate": 30,
    "ignoredIds": [],
    "maxDistance": 4.0,
    "minimumNumberOfTags": 2,
    "fieldLayoutFile": "2026-rebuilt-welded.json",
    "confidencePreset": 1,
    "noiseScale": 1.0
  },
  "enableDebugLogging": false,
  "timestamp": 1765759907
}
```

The values shown are the defaults.

| Field | Meaning |
|---|---|
| `teamNumber` | Team number used to find the robot. It is `-1` while a debug IP override is set. |
| `debugIpOverride` | IP address the app connects to instead of the team address, or `""` when unset. |
| `allowedPoseResetTimeoutMs` | Pose reset commands older than this are rejected. |
| `enableHighQualityStream` | Allows passthrough stream modes above 640x480 at 30 fps. |
| `streamMode` | Passthrough stream resolution, frame rate and JPEG quality. |
| `aprilTagDetectorMode.mode` | Detector mode. Only `0` (traditional) is implemented. |
| `maxDistance` | Detections whose average tag distance is above this many meters are dropped. |
| `minimumNumberOfTags` | Tags required in one frame before it is used. |
| `fieldLayoutFile` | Selected field layout. |
| `confidencePreset` | `0` Permissive, `1` Balanced, `2` Strict, `3` Debug. See [Kalman filter](../4-apriltag-tracking/4-kalman-filter.md#confidence-presets). |
| `noiseScale` | AprilTag trust multiplier from 0.5 to 2.0. Higher values trust tags less. |
| `timestamp` | Unix time in seconds. |

**Errors:** 500.

### POST /api/config

Changes one or more configuration values. Every field is optional; send only what you want to change. Field
names are matched case-insensitively. The body has the same shape as the `GET` response, without `success` and
`timestamp`.

```json
{ "teamNumber": 5152 }
```

```json
{
  "enableAprilTagDetector": true,
  "aprilTagDetectorMode": {
    "mode": 0,
    "width": 1280,
    "height": 960,
    "framerate": 30,
    "ignoredIds": [],
    "maxDistance": 4.0,
    "minimumNumberOfTags": 2,
    "fieldLayoutFile": "2026-rebuilt-welded.json",
    "confidencePreset": 1,
    "noiseScale": 1.0
  }
}
```

**Behavior**

- **`aprilTagDetectorMode` is all or nothing.** It replaces the whole detector mode, so send every field. The
  exceptions are `fieldLayoutFile`, `confidencePreset` and `noiseScale`: when one of these is missing or empty,
  its current value is kept.
- **Field layout changes take effect after a restart.** The app reads the layout only at startup. Every other
  detector setting applies right away.
- **Team number and override are linked.** Setting `teamNumber` clears `debugIpOverride`, and setting
  `debugIpOverride` sets the team number to `-1`. An empty `debugIpOverride` is ignored, so to clear an override
  you set the team number.
- **Some invalid values are ignored, not rejected.** An invalid `teamNumber` (outside 1 to 25599) or
  `debugIpOverride` (not a dotted IPv4 address) is logged as an error and ignored, but the request still returns
  200.

**Validation (400)**

| Check | Message starts with |
|---|---|
| `mode` is anything but `0` | `AprilTag detector mode N is not supported.` |
| `ignoredIds` has more than 64 entries, or an ID outside 0 to 586 | `ignoredIds has N entries` / `ignoredIds contains N` |
| `width`/`height`/`framerate` is not in [`/api/apriltag-video-modes`](#get-apiapriltag-video-modes) | `Resolution WxH @ Ffps is not supported on this camera.` |
| `minimumNumberOfTags` is not 1, 2, 3 or 4 | `minimumNumberOfTags=N is not in the supported set` |
| `fieldLayoutFile` is not a bundled layout or an existing custom layout | `Unknown field layout '...'.` |
| `confidencePreset` is outside 0 to 3 | `confidencePreset=N is out of range.` |
| `noiseScale` is not a finite number from 0.5 to 2.0 | `noiseScale=N is out of range.` |
| The body is the JSON literal `null` | `Invalid request` |

A body that is not valid JSON returns 500 with the parser's message.

The `mode` and `ignoredIds` checks run before anything is saved. The other checks run while the request is being
applied, so the fields applied before the failing check stay saved:

| Failing check | Already saved |
|---|---|
| Resolution or minimum tags | `teamNumber`, `debugIpOverride`, `allowedPoseResetTimeoutMs`, `enableAutoStartOnBoot`, `enablePassthroughStream`, `enableHighQualityStream`, `streamMode` |
| Field layout | Those fields, plus the other detector fields: `mode`, `width`, `height`, `framerate`, `ignoredIds`, `maxDistance`, `minimumNumberOfTags` |
| Confidence preset | Those fields, plus `fieldLayoutFile` |
| Noise scale | Those fields, plus `fieldLayoutFile` and `confidencePreset` |

**Success:** 200 `{ "success": true, "message": "Configuration updated" }`.

**Errors:** 400, 403, 413, 415, and 500 if saving fails.

### POST /api/reset-config

Resets every setting to its default.

**Success:** 200 `Configuration reset to defaults`.

**Errors:** 403, 500.

### GET /api/download-database

Downloads the configuration database as `config.db`. The response has `Content-Type: application/octet-stream`
and `Content-Disposition: attachment; filename="config.db"`.

**Errors:** 404 `Database file not found`, 500.

### POST /api/upload-database

Replaces the configuration database with the raw SQLite file sent as the request body. Restart the app to load it.

- **Content-Type** must be `application/octet-stream`, `application/vnd.sqlite3` or `application/x-sqlite3`,
  otherwise you get **415** `Content-Type must be application/octet-stream.`
- **Size limit:** 4 MB, otherwise **413**.

```bash
curl -X POST --data-binary @config.db -H "Content-Type: application/octet-stream" \
  http://<quest-ip>:5801/api/upload-database
```

**Success:** 200 `Database uploaded. Restart app to apply changes.`

**400** keeps the current configuration and reports the reason. The message starts with
`Database not uploaded:` and is returned when:

- the body is empty
- the file is not a SQLite database (it lacks the `SQLite format 3` header)
- the file fails validation

**Errors:** 403, 413, 415, 500.

## Status and control

### GET /api/info

```json
{
  "appName": "QuestNav",
  "version": "<app version>",
  "unityVersion": "<Unity version>",
  "buildDate": "yyyy-MM-dd HH:mm:ss",
  "platform": "Android",
  "deviceModel": "<headset model>",
  "operatingSystem": "<Android version>",
  "connectedClients": 1,
  "serverPort": 5801,
  "timestamp": 1765759907
}
```

`connectedClients` counts the IP addresses that called the API in the last 30 seconds. `buildDate` is the
modification time of the installed app package, which is not necessarily when it was built.

### GET /api/status

Live state. The app refreshes it three times a second.

| Field | Meaning |
|---|---|
| `position` | `{x, y, z}` field pose in meters (FRC axes) |
| `rotation` | `{x, y, z, w}` quaternion |
| `eulerAngles` | `{pitch, yaw, roll}` in degrees |
| `isTracking`, `trackingLostEvents` | Headset tracking state and how many times it was lost |
| `batteryPercent`, `batteryLevel`, `batteryStatus`, `batteryCharging` | Battery |
| `networkConnected` | Whether the app is connected to a NetworkTables server |
| `ipAddress` | The headset's IP address |
| `teamNumber` | Configured team number (`-1` while an IP override is set) |
| `robotIpAddress` | Address of the connected NetworkTables server. While disconnected: the IP override if set, otherwise `10.TE.AM.2`, otherwise empty. |
| `fps`, `frameCount` | App frame rate and frame counter |
| `connectedClients` | Web clients seen in the last 30 seconds |
| `passthroughResolutionLockedByAprilTag` | `true` while the AprilTag detector controls the camera resolution |
| `effectivePassthroughResolution` | `{width, height}` the stream actually uses, or `null` |
| `timestamp` | Unix seconds |

### GET /api/logs

Returns the most recent log messages, oldest first. The app keeps the last 500 messages.

| Query parameter | Default | Meaning |
|---|---|---|
| `count` | 100 | Number of messages to return. A value that is not a number returns none. |

```json
{
  "success": true,
  "logs": [
    { "Message": "Server started at http://...", "StackTrace": "", "Type": "Log", "Timestamp": 1765759907123 }
  ]
}
```

The entry keys are PascalCase. `Timestamp` is in Unix milliseconds.

### DELETE /api/logs

Clears the log buffer.

**Success:** 200 `Logs cleared`.

### POST /api/restart

Restarts the app after the response is sent.

**Success:** 200 `Restart initiated`.

### POST /api/reset-pose

Resets the pose to the field origin: position (0, 0, 0), heading 0. It goes through the same path as a robot
`setPose` command, with these differences:

- there is no latency compensation
- the result is not checked against the field bounds

Afterwards the alignment state is `POSE_RESET` and the reset epoch changes.

**Success:** 200 `Pose reset initiated`.

## Camera

### GET /api/video-modes

Lists the modes the passthrough stream can use:

```json
[ { "width": 320, "height": 240, "framerate": 24 } ]
```

**Errors:** 503 `Passthrough stream not available`, or 503 `Stream not initialized. Enable passthrough stream first.`

### GET /api/apriltag-video-modes

Lists the `width`/`height`/`framerate` combinations the AprilTag detector accepts: every camera resolution the
headset reports, combined with every frame rate in `1, 5, 15, 24, 30, 48, 60`. If the headset reports no
resolutions, the list uses 1280x1280 and 1280x960.

### GET /video

MJPEG stream of the passthrough camera (`Content-Type: multipart/x-mixed-replace; boundary=--frame`). Each part
is a JPEG image with its own `Content-Type` and `Content-Length`. Turn the stream on in the **Camera** tab first.

| Query parameter | Meaning |
|---|---|
| `resolution` | `WIDTHxHEIGHT` |
| `fps` | Frame rate |
| `compression` | JPEG quality from 1 to 100 (clamped) |

These parameters change the mode for every viewer. The change is not saved, and lasts until the stream settings
change or the app restarts.

While the AprilTag detector is running, it has priority over the camera, so the stream uses the detector's
resolution.

**Errors:**

- 503 `The stream is unavailable` if the app has no camera source for the stream
- 204 if the stream provider has not been set up

`/video` is outside `/api`, so the origin check does not apply.

## AprilTag field layouts

The app ships two layouts, `2026-rebuilt-welded.json` (the default) and `2025-reefscape-welded.json`. You can add
your own. Custom layouts are stored on the headset and are looked up before the bundled ones. See
[Field layout](../4-apriltag-tracking/5-field-layout-and-native-interop.md) for the file format.

**Layout names**

- **Allowed characters:** letters, digits, `.`, `_` and `-`.
- **Length:** at most 64 characters before `.json`.
- **First character:** must not be `.`.
- **No `..`:** a name must not contain `..` anywhere.
- **Extension:** `.json` is added when missing, but only to the POST `name` and the PATCH `newName`. The `{name}` in a
  URL must be the full file name, such as `practice-field.json`.
- **Bundled names:** a custom layout cannot use one (compared case-insensitively).

**Applying a change**

The selected layout is loaded only when the app starts. Uploading, renaming or deleting the layout in use takes
effect after a restart.

### GET /api/apriltag-field-layouts

```json
[
  { "fileName": "2026-rebuilt-welded.json", "displayName": "2026 rebuilt welded", "source": "bundled", "tagCount": 32 },
  { "fileName": "practice-field.json", "displayName": "practice field", "source": "custom", "tagCount": 8 }
]
```

`displayName` is the file name without `.json`, with `-` and `_` replaced by spaces.

### POST /api/apriltag-field-layouts

Creates a custom layout, or replaces one with the same name. Limit: 1 MB.

```json
{ "name": "practice-field", "content": "{ \"tags\": [ ... ], \"field\": { \"length\": 16.5, \"width\": 8.1 } }" }
```

`content` is the WPILib `AprilTagFieldLayout` JSON, sent as a string. It must parse, contain at least one tag,
give every tag a pose with finite numbers, and have no duplicate IDs.

**Success:** 200 `{ "success": true, "fileName": "practice-field.json", "tagCount": 8 }`.

**400** when:

- the body is not valid JSON
- `name` or `content` is missing or empty
- the name is invalid
- the name is a bundled layout's name
- the content fails validation

**Errors:** 403, 413, 415, 500.

### GET /api/apriltag-field-layouts/\{name\}

Returns the stored JSON of a custom layout. Bundled layouts can't be downloaded.

**Errors:**

| Status | When |
|---|---|
| 400 | The name is empty or invalid |
| 403 | The name is a bundled layout |
| 404 | No such custom layout |
| 500 | The file could not be read |

### PATCH /api/apriltag-field-layouts/\{name\}

Renames a custom layout.

Request: `{ "newName": "practice-field-2" }`.

If the renamed layout was selected, the configuration is updated to the new name.

**Success:**

```json
{ "success": true, "oldFileName": "practice-field.json", "newFileName": "practice-field-2.json", "reselectedConfigPointer": true }
```

**Errors:**

| Status | When |
|---|---|
| 400 | Invalid name, or `newName` missing or invalid |
| 403 | The name is empty or a bundled layout |
| 404 | No such custom layout |
| 409 | A layout named `newName` already exists |
| 413, 415, 500 | See the common rules |

### DELETE /api/apriltag-field-layouts/\{name\}

Deletes a custom layout.

**Success:**

```json
{ "success": true, "deletedFileName": "practice-field.json", "fellBackTo": "2026-rebuilt-welded.json" }
```

If the deleted layout was selected, the configuration falls back to `2026-rebuilt-welded.json`, and `fellBackTo`
names it. Otherwise `fellBackTo` is `null`.

**Errors:**

| Status | When |
|---|---|
| 400 | Invalid name |
| 403 | The name is empty or a bundled layout |
| 404 | No such custom layout |
| 500 | The file could not be deleted |
