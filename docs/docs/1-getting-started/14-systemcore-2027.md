---
title: SystemCore and WPILib 2027
---

# SystemCore and WPILib 2027

QuestNav supports both 2026 robots (roboRIO, WPILib 2026) and 2027 robots (SystemCore, WPILib 2027). The same app runs
on the headset for both, while the robot side needs the QuestNavLib line for its WPILib year.

:::warning Not yet validated on SystemCore hardware
SystemCore support is built against WPILib `2027.0.0-alpha-8`, both the app's NetworkTables library and the 2027
QuestNavLib. It has not been tested on a SystemCore yet. Treat the SystemCore connection details below as expected
behavior, and report what you see on the [QuestNav Discord](https://discord.gg/CsRjKfn8Xa) or
[GitHub](https://github.com/QuestNav/QuestNav/issues).
:::

## One App for Both Years

Install the latest QuestNav app no matter which robot it goes on. The app talks to both QuestNavLib lines over the
same NetworkTables topics:

- **Old libraries keep working.** Fields added in later protocol versions are ignored by older libraries.
- **New libraries detect old apps.** The 2027 library works out what an older app can do from its protocol version.

## Robot Addresses

With a team number set, the app tries these NetworkTables (NT4, port 5810) server addresses at the same time and
uses whichever answers:

| Address | Robot |
|---|---|
| `10.TE.AM.2` | Robot controller on the robot network (2026 and 2027). Tried only for team numbers 1 to 25599. |
| `172.27.0.1` | SystemCore over USB |
| `172.30.0.1` | SystemCore Wi-Fi access point |

- **mDNS is not used.** The Quest does not use mDNS discovery, and the roboRIO host-name fallbacks are gone. A
  2026 robot is therefore reachable only at `10.TE.AM.2`, or through a **Debug IP Override**.
- **The override wins.** With an override set, the app connects only to that address. See
  [Testing & Simulation](./7-simulation.md) for setting and clearing it.
- **Check the connection.** The **Status** tab of the web interface shows which address it connected to.

## Which QuestNavLib to Use

| | 2026 robot | 2027 robot |
|---|---|---|
| Robot controller | roboRIO | SystemCore |
| WPILib | 2026, Java 17 | 2027, Java 25 |
| WPILib packages | `edu.wpi.first.*` | `org.wpilib.*` |
| QuestNavLib releases | `2026-*` (newest: `2026-3.0.0-beta`) | 2027 releases |
| Vendordep year key | `"frcYear": "2026"` | `"wpilibYear": "2027_alpha7"` |
| Alignment state, reset epoch, protocol version API | No | Yes |
| Version check | Warns every 5 s when the release versions differ | Warns once if the app's protocol version is unsupported |

### 2026 robots

Install the vendordep from [Robot Code Setup](./8-robot-code.mdx#installation-steps).

- **No protocol version 2 API.** The 2026 library reads poses, tracking, battery and command results as before, but
  has no `getAlignmentState()`, `getResetEpoch()` or `getQuestNavProtocolVersion()`.
- **Version warning.** The 2026 library compares its release version with the app's, so with the shared app it
  usually warns every 5 seconds. If that is the only warning, `questNav.setVersionCheckEnabled(false)` silences it.

### 2027 robots

Download `questnavlib.json` from a QuestNav release on the
[releases page](https://github.com/QuestNav/QuestNav/releases) whose file has a `wpilibYear` key. Then copy it into
your project's `vendordeps` folder, or install its `jsonUrl` with **WPILib: Manage Vendor Libraries**.

- **No release yet.** At the time of writing no 2027 QuestNavLib release has been published. Until one is, build
  the library yourself; see [Until a 2027 release is published](#until-a-2027-release-is-published).
- **Matching the WPILib release.** The library is built against WPILib `2027.0.0-alpha-8`, so install that
  release. Its year key is `2027_alpha7`, which is the vendordep's `wpilibYear`, so the vendordep loads in a
  `2027.0.0-alpha-8` project.
- **Imports.** Code is the same as on a 2026 robot apart from the WPILib imports; see
  [2026 and 2027 imports](./8-robot-code.mdx#2026-and-2027-imports).

#### Until a 2027 release is published

Build QuestNavLib from the `main` branch of the [QuestNav repository](https://github.com/QuestNav/QuestNav) and
publish it into the Maven folder of your WPILib 2027 install, which robot projects already search:

1. Generate the Java protobuf classes as in
   [Development Setup](../2-development/1-development-setup.mdx#java-quickbuf).
2. With `JAVA_HOME` set to a JDK 25, run this from the `questnav-lib` folder:
   ```bash
   ./gradlew publishAllPublicationsToWpilibLocalRepository -PwpilibLocalMaven=<WPILib Maven folder>
   ```
   The folder is `C:\Users\Public\wpilib\2027_alpha7\maven` on Windows, `$HOME/.wpilib/2027_alpha7/maven` on
   macOS, and `$HOME/.local/share/wpilib/2027_alpha7/maven` on Linux (`$XDG_DATA_HOME/wpilib/2027_alpha7/maven` if
   `XDG_DATA_HOME` is set). Write `$HOME`, not `~`: the shell doesn't expand `~` after `-PwpilibLocalMaven=`.
3. Copy `questnav-lib/build/generated/vendordeps/questnavlib.json` into your robot project's `vendordeps` folder.

This builds version `2027-1.0.0-dev`. Rebuild to update it, and switch to a release's `questnavlib.json` once one is
published.

## Dashboards on 2026 Robots

QuestNav's `/QuestNav/frameData` and `/QuestNav/deviceData` topics use the `data.proto` schema, which imports
WPILib's geometry types from `wpimath/protobuf/geometry3d.proto` (the 2027 file name). The app publishes the
`data.proto` schema but leaves the geometry file for the robot to publish:

- **2027 robots** publish it as `wpimath/protobuf/geometry3d.proto` (the 2027 QuestNavLib's command publisher does so
  itself), so dashboards can decode QuestNav's topics.
- **2026 robots** publish the same types (`wpi.proto.ProtobufPose3d` and friends) as `geometry3d.proto`. The
  import does not resolve, so AdvantageScope, Glass or OutlineViewer may show `frameData` and `deviceData` as
  undecodable.

What is and isn't affected:

- **Robot code is not affected.** QuestNavLib decodes the messages with compiled classes, and the bytes on the wire
  are unchanged. Log the values you need from robot code, such as `isConnected()`, `isTracking()` or the robot pose,
  to see them on a 2026 dashboard.
- **Other topics decode normally.** `/QuestNav/version`, `/QuestNav/protocolVersion` and the camera stream topics
  are plain values.
- **Command topics.** The app doesn't publish the schema for `/QuestNav/request` and `/QuestNav/response`
  (`commands.proto`), because it crashes 2026 Glass and the simulation GUI
  ([allwpilib#8553](https://github.com/wpilibsuite/allwpilib/issues/8553)).

## Still to Be Verified on Hardware

- **Connecting to a SystemCore** through the robot network (`10.TE.AM.2`), USB (`172.27.0.1`) and its Wi-Fi access
  point (`172.30.0.1`)
- **Dashboards decoding `frameData`** on a SystemCore, and the extent of the 2026 dashboard limitation above
- **A 2026 roboRIO robot** with the current app and the 2026 QuestNavLib, end to end
