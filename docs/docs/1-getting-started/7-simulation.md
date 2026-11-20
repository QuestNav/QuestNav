---
title: Testing & Simulation 
---

## Reference Example Project

The [QuestNav Robot Sim Example](https://github.com/juchong/QuestNav-Robot-Sim-Example) is a complete WPILib project that runs in the desktop simulator out of the box. It demonstrates the full integration pattern covered throughout these docs:

- `ROBOT_TO_QUEST` mounting transform
- Pose fusion with `DifferentialDrivePoseEstimator` via `addVisionMeasurement`
- All lifecycle callbacks (connection, tracking, low-battery, command responses)
- Field-bounds rejection of bad pose readings
- Pose seeding on `autonomousInit`
- AdvantageScope-friendly NetworkTables telemetry

:::tip
Clone the example and run `./gradlew simulateJava` for a known-good baseline before integrating QuestNav into your own robot project. It's also a useful comparison point when troubleshooting your own integration.
:::

## Connecting to PC

Once your team number has been configured, QuestNav looks for a NetworkTables server at `10.TE.AM.2` (and, for a
SystemCore, at its USB and access point addresses; see [SystemCore and WPILib 2027](./14-systemcore-2027.md)). To
reach a simulation on your PC instead, point it at a specific IP address:

1. Open a browser to `http://<Quest IP Address>:5801`
2. Select the **Settings** tab
3. Enter your PC's IPv4 address in **Debug IP Override** and apply it

<img src="/img/web-interface/DebugIP.webp" width="800"/>

While an override is set, QuestNav connects only to that address, the team number shows as overridden, and the
settings show a **DEBUG MODE** badge.

:::warning
Clear the override before connecting the headset to a real robot, or the Quest will keep trying to reach your PC.
Emptying the **Debug IP Override** field does **not** clear it: an empty value is ignored. To clear it, enter your
team number in **Settings > Team Number** and apply it (or set the team number on the headset with **Set**). Setting
the team number removes the override.
:::

:::info
During simulation, the Quest needs network access to reach your development machine. If you disabled Wi-Fi during device setup (as recommended for competition), you will need to **temporarily re-enable Wi-Fi** on the headset so it can connect to the same network as your PC. Alternatively, connect both the Quest and your PC via Ethernet to the same switch or router. Remember to disable Wi-Fi again before competition.
:::

## WPILib Desktop Simulation

QuestNav works with WPILib's desktop simulation mode (`simulateJava` / `simulateNative`). This allows you to test
QuestNav integration without a robot controller by running your robot code on your development machine.

### Setup

1. **Start the simulation**: run `.\gradlew simulateJava` from your robot project directory in a terminal
2. **Configure the Quest**: set the `Debug IP Override` on the Quest's web interface (`http://<Quest IP>:5801`) to
   your development machine's IP address on the same network as the Quest
3. **Verify connection**: check that the `QuestNav` table appears and `QuestNav/frameData` updates in the WPILib
   simulation GUI's NetworkTables viewer or in AdvantageScope, or log `questNav.isConnected()` from robot code. The
   **Status** tab of the Quest's web interface also shows the address it connected to.

:::tip
The Quest and your development machine must be on the same network. To find your PC's IP address:
- **Windows:** Run `ipconfig` in a Command Prompt and look for the `IPv4 Address` under your active network adapter.
- **macOS:** Run `ipconfig getifaddr en0` in Terminal.
- **Linux:** Run `ip addr` or `hostname -I` in a terminal.
:::

### Windows Firewall

Windows Firewall may block incoming connections to the Java process running the simulation. When you first run
`simulateJava`, Windows will typically show a firewall prompt asking to allow `java.exe` through. **Click "Allow access"**.

If you dismissed the prompt or the Quest still can't connect:

1. Open **Windows Security > Firewall & network protection > Allow an app through firewall**
2. Click **Change settings**, then **Allow another app...**
3. Browse to the WPILib JDK's `java.exe` (typically `C:\Users\Public\wpilib\<year>\jdk\bin\java.exe`)
4. Ensure both **Private** and **Public** checkboxes are selected
5. Restart the simulation

:::tip
If you have multiple JDK installations, make sure the one used by the simulation is allowed.
You can check which `java.exe` is in use via Task Manager while the simulation is running.
:::

### Port Conflicts

QuestNav connects over NT4 on port **5810**. The WPILib 2026 simulation's NetworkTables server also binds port
**1735** (NT3). If a previous simulation instance didn't shut down cleanly, these ports may still be in use. Open Task Manager, look for stale `java.exe`
processes, and end them before restarting the simulation.
