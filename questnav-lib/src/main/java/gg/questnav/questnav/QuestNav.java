/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import gg.questnav.questnav.protos.generated.Commands;
import gg.questnav.questnav.protos.generated.Data;
import gg.questnav.questnav.protos.wpilib.CommandProto;
import gg.questnav.questnav.protos.wpilib.CommandResponseProto;
import gg.questnav.questnav.protos.wpilib.DeviceDataProto;
import gg.questnav.questnav.protos.wpilib.FrameDataProto;
import java.util.LinkedHashSet;
import java.util.OptionalDouble;
import java.util.OptionalInt;
import java.util.Set;
import java.util.concurrent.ThreadLocalRandom;
import java.util.function.Consumer;
import java.util.function.IntConsumer;
import org.wpilib.driverstation.DriverStationErrors;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.proto.Pose3dProto;
import org.wpilib.math.geometry.proto.detail.ProtobufPose3d;
import org.wpilib.networktables.IntegerSubscriber;
import org.wpilib.networktables.NetworkTable;
import org.wpilib.networktables.NetworkTableInstance;
import org.wpilib.networktables.ProtobufPublisher;
import org.wpilib.networktables.ProtobufSubscriber;
import org.wpilib.networktables.PubSubOption;
import org.wpilib.networktables.StringSubscriber;
import org.wpilib.system.Timer;

/**
 * The QuestNav class provides a comprehensive interface for communicating with an Oculus/Meta Quest
 * VR headset for robot localization and tracking in FRC robotics applications.
 *
 * <p>This class handles all aspects of Quest-robot communication including:
 *
 * <ul>
 *   <li>Real-time pose tracking and localization data
 *   <li>Command sending and response handling
 *   <li>Device status monitoring (battery, tracking state, connectivity)
 *   <li>NetworkTables-based communication protocol
 *   <li>Event-driven callbacks for connection, tracking, battery, and command state changes
 * </ul>
 *
 * <h2>Basic Usage</h2>
 *
 * <pre>{@code
 * // Create QuestNav instance
 * QuestNav questNav = new QuestNav();
 *
 * // Register callbacks
 * questNav.onConnected(() -> System.out.println("Quest connected!"));
 * questNav.onDisconnected(() -> DriverStationErrors.reportWarning("Quest disconnected!", false));
 * questNav.onTrackingLost(() -> DriverStationErrors.reportWarning("Quest tracking lost!", false));
 * questNav.onTrackingAcquired(() -> System.out.println("Quest tracking acquired!"));
 * questNav.onLowBattery(20, level -> DriverStationErrors.reportWarning("Quest battery low: " + level + "%", false));
 *
 * // Set initial robot pose (required for field-relative tracking)
 * Pose2d initialPose = new Pose2d(1.0, 2.0, Rotation2d.fromDegrees(90));
 * questNav.setPose(initialPose);
 *
 * // In robot periodic methods
 * public void robotPeriodic() {
 *   questNav.commandPeriodic(); // Process command responses and fire callbacks
 *
 *   // Get latest pose data
 *   PoseFrame[] newFrames = questNav.getAllUnreadPoseFrames();
 *   for (PoseFrame frame : newFrames) {
 *     // Use frame.questPose() and frame.dataTimestamp() with pose estimator
 *   }
 * }
 * }</pre>
 *
 * <h2>Coordinate Systems</h2>
 *
 * <p>QuestNav uses the WPILib field coordinate system:
 *
 * <ul>
 *   <li><strong>X-axis:</strong> Forward direction (towards opposing alliance)
 *   <li><strong>Y-axis:</strong> Left direction (when facing forward)
 *   <li><strong>Rotation:</strong> Counter-clockwise positive (standard mathematical convention)
 *   <li><strong>Units:</strong> Meters for translation, radians for rotation
 * </ul>
 *
 * <h2>Threading and Performance</h2>
 *
 * <p>This class is designed for use in FRC robot code and follows WPILib threading conventions:
 *
 * <ul>
 *   <li>All methods are thread-safe for typical FRC usage patterns
 *   <li>Uses cached objects to minimize garbage collection pressure
 *   <li>NetworkTables handles the underlying communication asynchronously
 *   <li>Call {@link #commandPeriodic()} regularly to process command responses and fire callbacks
 * </ul>
 *
 * <h2>Error Handling</h2>
 *
 * <p>The class provides robust error handling:
 *
 * <ul>
 *   <li>Methods return {@link java.util.Optional} types when data might not be available
 *   <li>Connection status can be checked with {@link #isConnected()}
 *   <li>Tracking status can be monitored with {@link #isTracking()}
 *   <li>Command failures are reported through DriverStation error logging and via {@link
 *       #onCommandFailure(Consumer)}
 * </ul>
 *
 * @see PoseFrame
 * @see org.wpilib.math.geometry.Pose2d
 * @see org.wpilib.networktables.NetworkTableInstance
 * @since 2025.1.0
 * @author QuestNav Team
 */
public class QuestNav {

  /**
   * Wire-protocol version this library speaks. The QuestNav app publishes its own on the {@code
   * protocolVersion} topic; apps without the topic are version 1. Keep in step with
   * QuestNavConstants.Network.PROTOCOL_VERSION in the app and the version history in data.proto.
   */
  public static final int PROTOCOL_VERSION = 2;

  /** Oldest QuestNav app protocol version this library works with */
  public static final int MIN_SUPPORTED_PROTOCOL_VERSION = 1;

  /** Protocol version of QuestNav apps that don't publish one */
  private static final int LEGACY_PROTOCOL_VERSION = 1;

  /** Number of sent commands remembered while waiting for their responses */
  private static final int MAX_PENDING_COMMANDS = 32;

  /** Time without a new frame after which the Quest is considered disconnected */
  private static final double CONNECTION_TIMEOUT_SECONDS = 0.120;

  /** Battery percentage at or below which {@link #onLowBatteryCallback} fires */
  private int lowBatteryThreshold = 20;

  /** NetworkTable instance used for communication */
  private final NetworkTableInstance nt4Instance = NetworkTableInstance.getDefault();

  /** NetworkTable for Quest navigation data */
  private final NetworkTable questNavTable = nt4Instance.getTable("QuestNav");

  /** Protobuf instance for CommandResponse */
  private final CommandResponseProto commandResponseProto = new CommandResponseProto();

  /** Protobuf instance for Command */
  private final CommandProto commandProto = new CommandProto();

  /** Protobuf instance for Pose3d */
  private final Pose3dProto pose3dProto = new Pose3dProto();

  /** Protobuf instance for device data */
  private final DeviceDataProto deviceDataProto = new DeviceDataProto();

  /** Protobuf instance for frame data */
  private final FrameDataProto frameDataProto = new FrameDataProto();

  /** Subscriber for command response */
  private final ProtobufSubscriber<Commands.ProtobufQuestNavCommandResponse> responseSubscriber =
      questNavTable
          .getProtobufTopic("response", commandResponseProto)
          .subscribe(
              Commands.ProtobufQuestNavCommandResponse.newInstance(),
              PubSubOption.periodic(0.05),
              PubSubOption.SEND_ALL,
              PubSubOption.pollStorage(20));

  /** Subscriber for frame data */
  private final ProtobufSubscriber<PoseFrame> frameDataSubscriber =
      questNavTable
          .getProtobufTopic("frameData", frameDataProto)
          .subscribe(
              new PoseFrame(new Pose3d(), 0, 0, 0, false),
              PubSubOption.periodic(0.01),
              PubSubOption.SEND_ALL,
              PubSubOption.pollStorage(20));

  /** Subscriber for device data */
  private final ProtobufSubscriber<Data.ProtobufQuestNavDeviceData> deviceDataSubscriber =
      questNavTable
          .getProtobufTopic("deviceData", deviceDataProto)
          .subscribe(Data.ProtobufQuestNavDeviceData.newInstance());

  /** Subscriber for QuestNav app version */
  private final StringSubscriber versionSubscriber =
      questNavTable.getStringTopic("version").subscribe("unknown");

  /** Subscriber for the QuestNav app's wire-protocol version */
  private final IntegerSubscriber protocolVersionSubscriber =
      questNavTable.getIntegerTopic("protocolVersion").subscribe(0);

  /** Publisher for command requests */
  private final ProtobufPublisher<Commands.ProtobufQuestNavCommand> requestPublisher =
      questNavTable.getProtobufTopic("request", commandProto).publish();

  /** Cached request to lessen GC pressure */
  private final Commands.ProtobufQuestNavCommand cachedCommandRequest =
      Commands.ProtobufQuestNavCommand.newInstance();

  /** Cached pose reset request to lessen GC pressure */
  private final Commands.ProtobufQuestNavPoseResetPayload cachedPoseResetPayload =
      Commands.ProtobufQuestNavPoseResetPayload.newInstance();

  /** Cached proto pose (for reset requests) to lessen GC pressure */
  private final ProtobufPose3d cachedProtoPose = ProtobufPose3d.newInstance();

  /**
   * Last sent command ID. Starts at a random value so a response left on the topic from an earlier
   * robot program run is not taken for one of this run's commands.
   */
  private int lastSentRequestId = ThreadLocalRandom.current().nextInt();

  /** IDs of sent commands that have not had a response yet, oldest first */
  private final Set<Integer> pendingCommandIds = new LinkedHashSet<>();

  /** True to warn when the QuestNav app's protocol version is not supported */
  private boolean versionCheckEnabled = true;

  /** The app protocol version last checked, so each one is only reported once */
  private int lastCheckedProtocolVersion = -1;

  /** Reports version warnings; replaced in tests */
  Consumer<String> warningReporter = message -> DriverStationErrors.reportWarning(message, false);

  // Callback state tracking

  /** Cached connection state from the previous commandPeriodic() call */
  private boolean lastConnectedState = false;

  /** Cached tracking state from the previous commandPeriodic() call */
  private boolean lastTrackingState = false;

  /** Whether the low-battery callback has already fired for the current threshold crossing */
  private boolean lowBatteryFired = false;

  // Callbacks

  /** Fired once when the Quest transitions from disconnected → connected */
  private Runnable onConnectedCallback = null;

  /** Fired once when the Quest transitions from connected → disconnected */
  private Runnable onDisconnectedCallback = null;

  /** Fired once when the Quest transitions from not-tracking → tracking */
  private Runnable onTrackingAcquiredCallback = null;

  /** Fired once when the Quest transitions from tracking → not-tracking */
  private Runnable onTrackingLostCallback = null;

  /**
   * Fired once when battery drops at or below {@link #lowBatteryThreshold}. Resets when battery
   * rises above the threshold again.
   */
  private IntConsumer onLowBatteryCallback = null;

  /** Fired for each command response where {@code getSuccess() == true} */
  private Consumer<Commands.ProtobufQuestNavCommandResponse> onSuccessCallback = null;

  /** Fired for each command response where {@code getSuccess() == false} */
  private Consumer<Commands.ProtobufQuestNavCommandResponse> onFailureCallback = null;

  /**
   * Creates a new QuestNav instance for communicating with a Quest headset.
   *
   * <p>This constructor initializes all necessary NetworkTables subscribers and publishers for
   * communication with the Quest device. The instance is ready to use immediately, but you should
   * call {@link #setPose(Pose3d)} to establish field-relative tracking before relying on pose data.
   *
   * <p>The constructor sets up:
   *
   * <ul>
   *   <li>NetworkTables communication on the "QuestNav" table
   *   <li>Protobuf serialization for efficient data transfer
   *   <li>Cached objects to minimize garbage collection
   *   <li>Subscribers for frame data, device data, and command responses
   *   <li>Publisher for sending commands to the Quest
   * </ul>
   */
  public QuestNav() {}

  // Callback registration

  /**
   * Registers a callback that fires once when the Quest headset transitions from disconnected to
   * connected. The callback is evaluated each {@link #commandPeriodic()} call.
   *
   * @param callback Runnable to invoke on connection
   */
  public void onConnected(Runnable callback) {
    this.onConnectedCallback = callback;
  }

  /**
   * Registers a callback that fires once when the Quest headset transitions from connected to
   * disconnected. The callback is evaluated each {@link #commandPeriodic()} call.
   *
   * @param callback Runnable to invoke on disconnection
   */
  public void onDisconnected(Runnable callback) {
    this.onDisconnectedCallback = callback;
  }

  /**
   * Registers a callback that fires once when the Quest headset transitions from not-tracking to
   * actively tracking. The callback is evaluated each {@link #commandPeriodic()} call.
   *
   * @param callback Runnable to invoke when tracking is acquired
   */
  public void onTrackingAcquired(Runnable callback) {
    this.onTrackingAcquiredCallback = callback;
  }

  /**
   * Registers a callback that fires once when the Quest headset transitions from actively tracking
   * to not-tracking. The callback is evaluated each {@link #commandPeriodic()} call.
   *
   * @param callback Runnable to invoke when tracking is lost
   */
  public void onTrackingLost(Runnable callback) {
    this.onTrackingLostCallback = callback;
  }

  /**
   * Registers a callback that fires once when the Quest battery level drops at or below the given
   * threshold. The callback will not fire again until the battery rises above the threshold and
   * drops back down.
   *
   * @param thresholdPercent Battery percentage (0–100) at or below which the callback fires
   * @param callback IntConsumer receiving the current battery percentage when the threshold is
   *     crossed
   */
  public void onLowBattery(int thresholdPercent, IntConsumer callback) {
    this.lowBatteryThreshold = thresholdPercent;
    this.onLowBatteryCallback = callback;
  }

  /**
   * Registers a callback that fires for each command response where the command succeeded. Called
   * from {@link #commandPeriodic()}.
   *
   * @param callback Consumer receiving the raw {@link Commands.ProtobufQuestNavCommandResponse}
   */
  public void onCommandSuccess(Consumer<Commands.ProtobufQuestNavCommandResponse> callback) {
    this.onSuccessCallback = callback;
  }

  /**
   * Registers a callback that fires for each command response where the command failed. Called from
   * {@link #commandPeriodic()}. The failure is also reported to the DriverStation regardless of
   * whether a callback is registered.
   *
   * @param callback Consumer receiving the raw {@link Commands.ProtobufQuestNavCommandResponse}
   */
  public void onCommandFailure(Consumer<Commands.ProtobufQuestNavCommandResponse> callback) {
    this.onFailureCallback = callback;
  }

  // Internal helpers

  /**
   * Checks the QuestNav app's protocol version while connected, and reports a warning to the
   * DriverStation if this library doesn't support it. Each version is only reported once, so a
   * mismatch does not flood the console. The release versions are not compared, since the same app
   * release is used with the QuestNavLib releases for different WPILib years.
   *
   * @see #getQuestNavProtocolVersion()
   */
  private void checkProtocolVersion() {
    if (!versionCheckEnabled) {
      return;
    }
    var protocolVersion = getQuestNavProtocolVersion();
    if (protocolVersion.isEmpty() || protocolVersion.getAsInt() == lastCheckedProtocolVersion) {
      return;
    }
    lastCheckedProtocolVersion = protocolVersion.getAsInt();

    String warning =
        protocolVersionWarning(lastCheckedProtocolVersion, getQuestNavVersion(), getLibVersion());
    if (warning != null) {
      warningReporter.accept(warning);
    }
  }

  /**
   * Gets the warning for a QuestNav app protocol version this library doesn't support.
   *
   * @param appProtocolVersion The app's protocol version
   * @param appVersion The app's release version, for the message
   * @param libVersion This library's release version, for the message
   * @return The warning, or null when the version is supported
   */
  static String protocolVersionWarning(
      int appProtocolVersion, String appVersion, String libVersion) {
    if (appProtocolVersion < MIN_SUPPORTED_PROTOCOL_VERSION) {
      return String.format(
          "WARNING FROM QUESTNAV: The QuestNav app on your headset (%s) uses protocol version %d, "
              + "but QuestNavLib %s on your robot needs at least version %d. "
              + "Update the QuestNav app on your headset.",
          appVersion, appProtocolVersion, libVersion, MIN_SUPPORTED_PROTOCOL_VERSION);
    }
    if (appProtocolVersion > PROTOCOL_VERSION) {
      return String.format(
          "WARNING FROM QUESTNAV: The QuestNav app on your headset (%s) uses protocol version %d, "
              + "newer than QuestNavLib %s on your robot knows (up to %d). "
              + "Some features may not work until you update the QuestNavLib vendordep.",
          appVersion, appProtocolVersion, libVersion, PROTOCOL_VERSION);
    }
    return null;
  }

  /**
   * Gets the ID for the command after {@code lastId}, skipping 0, which a command that never set an
   * ID has.
   *
   * @param lastId The last command ID used
   * @return The next command ID
   */
  static int nextCommandId(int lastId) {
    int id = lastId + 1;
    return id == 0 ? 1 : id;
  }

  /**
   * Evaluates connection, tracking, and battery state against cached values and fires any
   * registered callbacks when state transitions are detected.
   */
  private void checkStateCallbacks() {
    // --- Connection state ---
    boolean connected = isConnected();
    if (connected != lastConnectedState) {
      if (connected) {
        if (onConnectedCallback != null) onConnectedCallback.run();
      } else {
        if (onDisconnectedCallback != null) onDisconnectedCallback.run();
      }
      lastConnectedState = connected;
    }

    // --- Tracking state ---
    boolean tracking = isTracking();
    if (tracking != lastTrackingState) {
      if (tracking) {
        if (onTrackingAcquiredCallback != null) onTrackingAcquiredCallback.run();
      } else {
        if (onTrackingLostCallback != null) onTrackingLostCallback.run();
      }
      lastTrackingState = tracking;
    }

    // --- Low battery ---
    if (onLowBatteryCallback != null) {
      getBatteryPercent()
          .ifPresent(
              level -> {
                if (level <= lowBatteryThreshold && !lowBatteryFired) {
                  onLowBatteryCallback.accept(level);
                  lowBatteryFired = true;
                } else if (level > lowBatteryThreshold) {
                  // Reset so the callback can fire again if battery dips below again
                  lowBatteryFired = false;
                }
              });
    }
  }

  // Public API

  /**
   * Turns the version check on or off. When on, a warning is reported to the DriverStation once if
   * the QuestNav app's protocol version is older than this QuestNavLib supports or newer than it
   * knows. Different release versions alone don't warn.
   *
   * @param enabled true to enable version checking, false to disable it. Default is true.
   * @see #getQuestNavProtocolVersion()
   */
  public void setVersionCheckEnabled(boolean enabled) {
    this.versionCheckEnabled = enabled;
  }

  /**
   * Sets the field-relative pose of the Quest headset by commanding it to reset its tracking.
   *
   * <p>This method sends a pose reset command to the Quest headset, telling it where the Quest is
   * currently located on the field. This is essential for establishing field-relative tracking and
   * should be called:
   *
   * <ul>
   *   <li>At the start of autonomous or teleop when the Quest position is known
   *   <li>When the robot (and Quest) is placed at a known location (e.g., against field walls)
   *   <li>After significant tracking drift is detected
   *   <li>When integrating with other localization systems (vision, odometry)
   * </ul>
   *
   * <p><strong>Important:</strong> This should be the Quest's pose, not the robot's pose. If you
   * know the robot's pose, you need to apply the mounting offset to get the Quest's pose before
   * calling this method.
   *
   * <p>Only the yaw of the pose's rotation is applied. The Quest keeps its own gravity-referenced
   * pitch and roll, so a pitch or roll in the pose does not tilt its tracking.
   *
   * <p>The command is sent asynchronously. Success or failure is reported via {@link
   * #onCommandSuccess(Consumer)} and {@link #onCommandFailure(Consumer)} respectively, and failures
   * are also logged to the DriverStation.
   *
   * <h4>Usage Example:</h4>
   *
   * <pre>{@code
   * // Set Quest pose at autonomous start (if you know the Quest's position directly)
   * Pose3d questPose = new Pose3d(1.5, 5.5, new Rotation3d(0.0, 0.0, 0.0)));
   * questNav.setPose(questPose);
   *
   * // If you know the robot pose, apply mounting offset to get Quest pose
   * Pose2d robotPose = poseEstimator.getEstimatedPosition();
   * Pose3d questPose = new Pose3d(robotPose).transformBy(mountingOffset);
   * questNav.setPose(questPose);
   * }</pre>
   *
   * @param pose The Quest's current field-relative pose in WPILib coordinates (meters for
   *     translation, radians for rotation)
   * @throws IllegalArgumentException if pose contains NaN or infinite values
   * @see #onCommandSuccess(Consumer)
   * @see #onCommandFailure(Consumer)
   * @see #isConnected()
   */
  public void setPose(Pose3d pose) {
    cachedProtoPose.clear();
    pose3dProto.pack(cachedProtoPose, pose);
    cachedCommandRequest.clear();
    lastSentRequestId = nextCommandId(lastSentRequestId);
    var requestToSend =
        cachedCommandRequest
            .setType(Commands.QuestNavCommandType.POSE_RESET)
            .setCommandId(lastSentRequestId)
            .setPoseResetPayload(cachedPoseResetPayload.clear().setTargetPose(cachedProtoPose));

    pendingCommandIds.add(lastSentRequestId);
    if (pendingCommandIds.size() > MAX_PENDING_COMMANDS) {
      // The oldest has most likely been answered while disconnected or dropped
      pendingCommandIds.remove(pendingCommandIds.iterator().next());
    }
    requestPublisher.set(requestToSend);
  }

  /**
   * Returns the Quest headset's current battery level as a percentage.
   *
   * <p>Battery level guidelines:
   *
   * <ul>
   *   <li><strong>80-100%:</strong> Excellent - full match capability
   *   <li><strong>50-80%:</strong> Good - normal operation expected
   *   <li><strong>20-50%:</strong> Fair - consider charging after match
   *   <li><strong>10-20%:</strong> Low - charge soon, monitor closely
   *   <li><strong>0-10%:</strong> Critical - immediate charging required
   * </ul>
   *
   * @return An {@link OptionalInt} containing the battery percentage (0-100), or empty if no device
   *     data is available or Quest is disconnected
   * @see #onLowBattery(int, IntConsumer)
   * @see #isConnected()
   */
  public OptionalInt getBatteryPercent() {
    if (!hasDeviceData()) {
      return OptionalInt.empty();
    }
    return OptionalInt.of(deviceDataSubscriber.get().getBatteryPercent());
  }

  /**
   * Gets the current frame count from the Quest headset.
   *
   * @return The frame count value, or empty if no frame data is available
   */
  public OptionalInt getFrameCount() {
    if (frameDataSubscriber.getLastChange() == 0) {
      return OptionalInt.empty();
    }
    return OptionalInt.of(frameDataSubscriber.get().frameCount());
  }

  /**
   * Gets the number of tracking lost events since the Quest connected to the robot.
   *
   * @return The tracking lost counter value, or empty if no device data is available or Quest is
   *     disconnected
   */
  public OptionalInt getTrackingLostCounter() {
    if (!hasDeviceData()) {
      return OptionalInt.empty();
    }
    return OptionalInt.of(deviceDataSubscriber.get().getTrackingLostCounter());
  }

  /**
   * Whether the Quest is connected and has published device data. The subscriber returns a default
   * instance until then, so a zero battery level would otherwise look like real data.
   */
  private boolean hasDeviceData() {
    return deviceDataSubscriber.getLastChange() != 0 && isConnected();
  }

  /**
   * Determines if the Quest headset is currently connected to the robot. Connection is determined
   * by how stale the last received frame from the Quest is.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  public boolean isConnected() {
    long lastChange = frameDataSubscriber.getLastChange();
    return lastChange != 0
        && Timer.getTimestamp() - ntTimeToSeconds(lastChange) < CONNECTION_TIMEOUT_SECONDS;
  }

  /**
   * Gets the latency of the Quest > Robot Connection. Returns the latency between the current time
   * and the last frame data update.
   *
   * @return The latency in milliseconds
   */
  public double getLatency() {
    return (Timer.getTimestamp() - ntTimeToSeconds(frameDataSubscriber.getLastChange())) * 1000.0;
  }

  /**
   * Returns the Quest app's uptime timestamp for debugging and diagnostics.
   *
   * <p><strong>Important:</strong> For integration with a pose estimator, use the timestamp from
   * {@link PoseFrame#dataTimestamp()} instead.
   *
   * @return An {@link OptionalDouble} containing the Quest app uptime in seconds, or empty if no
   *     frame data is available
   * @see PoseFrame#dataTimestamp()
   * @see #getAllUnreadPoseFrames()
   */
  public OptionalDouble getAppTimestamp() {
    if (frameDataSubscriber.getLastChange() == 0) {
      return OptionalDouble.empty();
    }
    return OptionalDouble.of(frameDataSubscriber.get().appTimestamp());
  }

  /**
   * Gets the current tracking state of the Quest headset.
   *
   * <p><strong>Important:</strong> When tracking is lost, pose data becomes unreliable and should
   * not be used for robot control. Implement fallback localization methods (wheel odometry, vision,
   * etc.) for when Quest tracking is unavailable.
   *
   * @return {@code true} if the Quest is connected and actively tracking, so pose data is reliable;
   *     {@code false} if tracking is lost or the Quest is disconnected
   * @see #onTrackingLost(Runnable)
   * @see #onTrackingAcquired(Runnable)
   * @see #isConnected()
   */
  public boolean isTracking() {
    // The last frame stays in the subscriber after the Quest disconnects
    return isConnected() && frameDataSubscriber.get().isTracking();
  }

  /**
   * Gets what the Quest's pose is currently referenced to: raw tracking, a pose reset, or AprilTag
   * alignment. Use it, for example, to only trust the Quest's field position once AprilTags have
   * aligned it.
   *
   * @return The latest frame's alignment state, or {@link AlignmentState#UNKNOWN} if the Quest is
   *     disconnected or its QuestNav app is too old to report it
   * @see PoseFrame#alignmentState()
   */
  public AlignmentState getAlignmentState() {
    if (!isConnected()) {
      return AlignmentState.UNKNOWN;
    }
    return frameDataSubscriber.get().alignmentState();
  }

  /**
   * Gets the Quest's current reset epoch, a counter that changes whenever its pose is re-placed
   * instead of following its motion (pose reset, AprilTag alignment, tracking relocalization).
   * Compare it with an earlier value to tell whether the pose has been re-placed since. AprilTag
   * corrections within an epoch don't change it; see {@link PoseFrame#resetEpoch()}.
   *
   * @return The latest frame's reset epoch, or empty if the Quest is disconnected or its QuestNav
   *     app is too old to report it
   * @see PoseFrame#resetEpoch()
   */
  public OptionalInt getResetEpoch() {
    if (!isConnected()) {
      return OptionalInt.empty();
    }
    PoseFrame frame = frameDataSubscriber.get();
    return isLegacyFrame(frame) ? OptionalInt.empty() : OptionalInt.of(frame.resetEpoch());
  }

  /**
   * Whether a frame lacks the fields protocol version 2 added, as from an app that predates them.
   * From version 2 on, apps set the alignment state in every frame; a nonzero epoch also marks a
   * newer app's frame whose state this library doesn't know.
   */
  private static boolean isLegacyFrame(PoseFrame frame) {
    return frame.alignmentState() == AlignmentState.UNKNOWN && frame.resetEpoch() == 0;
  }

  /**
   * Retrieves all new pose frames received from the Quest since the last call to this method.
   *
   * <p>This is the primary method for integrating QuestNav with FRC pose estimation systems. It
   * returns an array of {@link PoseFrame} objects containing pose data and timestamps that can be
   * fed directly into a {@link org.wpilib.math.estimator.PoseEstimator}.
   *
   * <p><strong>Important:</strong> This method consumes the frame queue, so each frame is only
   * returned once. Call this method regularly (every robot loop) to avoid missing frames.
   *
   * <h4>Integration with Pose Estimator:</h4>
   *
   * <pre>{@code
   * PoseFrame[] newFrames = questNav.getAllUnreadPoseFrames();
   * for (PoseFrame frame : newFrames) {
   *   if (questNav.isTracking() && questNav.isConnected()) {
   *     poseEstimator.addVisionMeasurement(
   *       frame.questPose(),
   *       frame.dataTimestamp(),
   *       VecBuilder.fill(0.1, 0.1, 0.05)
   *     );
   *   }
   * }
   * }</pre>
   *
   * @return Array of new {@link PoseFrame} objects received since the last call. Empty array if no
   *     new frames are available or Quest is disconnected.
   * @see PoseFrame
   * @see #isTracking()
   * @see #isConnected()
   */
  public PoseFrame[] getAllUnreadPoseFrames() {
    var frameDataArray = frameDataSubscriber.readQueue();
    var result = new PoseFrame[frameDataArray.length];
    for (int i = 0; i < result.length; i++) {
      var frameData = frameDataArray[i];
      var frame = frameData.value;
      if (frame.dataTimestamp() == 0) {
        // Older QuestNav apps don't send a capture time; fall back to the NT receive time
        frame =
            new PoseFrame(
                frame.questPose3d(),
                ntTimeToSeconds(frameData.serverTime),
                frame.appTimestamp(),
                frame.frameCount(),
                frame.isTracking(),
                frame.alignmentState(),
                frame.resetEpoch());
      }
      result[i] = frame;
    }
    return result;
  }

  /**
   * Converts a NetworkTables timestamp (integer nanoseconds) to seconds.
   *
   * @param ntTime NetworkTables timestamp in nanoseconds
   * @return The timestamp in seconds
   */
  private static double ntTimeToSeconds(long ntTime) {
    return ntTime * 1e-9;
  }

  /**
   * Processes command responses from the Quest headset, fires command success/failure callbacks,
   * and evaluates connection, tracking, and battery state callbacks.
   *
   * <p>This method must be called regularly (typically in {@code robotPeriodic()}) to:
   *
   * <ul>
   *   <li>Process responses to commands sent via {@link #setPose(Pose3d)}
   *   <li>Fire {@link #onCommandSuccess(Consumer)} and {@link #onCommandFailure(Consumer)}
   *       callbacks
   *   <li>Detect and fire connection state change callbacks ({@link #onConnected(Runnable)}, {@link
   *       #onDisconnected(Runnable)})
   *   <li>Detect and fire tracking state change callbacks ({@link #onTrackingAcquired(Runnable)},
   *       {@link #onTrackingLost(Runnable)})
   *   <li>Evaluate low battery threshold and fire {@link #onLowBattery(int, IntConsumer)} callback
   *   <li>Log command failures to the DriverStation
   *   <li>Prevent command response queue overflow
   * </ul>
   *
   * <h4>Usage Pattern:</h4>
   *
   * <pre>{@code
   * public class Robot extends TimedRobot {
   *   private QuestNav questNav = new QuestNav();
   *
   *   @Override
   *   public void robotPeriodic() {
   *     questNav.commandPeriodic(); // Call every loop
   *   }
   * }
   * }</pre>
   *
   * @see #setPose(Pose3d)
   * @see #onCommandSuccess(Consumer)
   * @see #onCommandFailure(Consumer)
   * @see #onConnected(Runnable)
   * @see #onDisconnected(Runnable)
   * @see #onTrackingAcquired(Runnable)
   * @see #onTrackingLost(Runnable)
   * @see #onLowBattery(int, IntConsumer)
   */
  public void commandPeriodic() {
    checkProtocolVersion();
    checkStateCallbacks();

    Commands.ProtobufQuestNavCommandResponse[] responses = responseSubscriber.readQueueValues();

    for (Commands.ProtobufQuestNavCommandResponse response : responses) {
      // Responses to other commands: an earlier run's, a duplicate, or another client's
      if (!pendingCommandIds.remove(response.getCommandId())) {
        continue;
      }
      if (response.getSuccess()) {
        if (onSuccessCallback != null) {
          onSuccessCallback.accept(response);
        }
      } else {
        DriverStationErrors.reportError(
            "QuestNav command failed!\n" + response.getErrorMessage(), false);
        if (onFailureCallback != null) {
          onFailureCallback.accept(response);
        }
      }
    }
  }

  /**
   * Retrieves the QuestNav-lib version number.
   *
   * @return The version number as a String.
   */
  public String getLibVersion() {
    return BuildConfig.APP_VERSION;
  }

  /**
   * Retrieves the QuestNav app version running on the Quest headset, for display and logging.
   * Compatibility is decided by {@link #getQuestNavProtocolVersion()}, not by this.
   *
   * @return The version number as a String, or "unknown" if unable to retrieve.
   */
  public String getQuestNavVersion() {
    return versionSubscriber.get();
  }

  /**
   * Gets the wire-protocol version of the QuestNav app on the headset. Apps that don't publish one
   * predate it and are version 1.
   *
   * @return The app's protocol version, or empty if the Quest is disconnected or its version is not
   *     known yet (or any more) while the app connects or disconnects
   * @see #PROTOCOL_VERSION
   * @see #MIN_SUPPORTED_PROTOCOL_VERSION
   */
  public OptionalInt getQuestNavProtocolVersion() {
    if (!isConnected()) {
      return OptionalInt.empty();
    }
    if (!protocolVersionSubscriber.exists()) {
      // NT announces and unpublishes an app's topics one at a time, so a current app's frames can
      // arrive before this topic or outlast it; only legacy frames mean a legacy app
      return isLegacyFrame(frameDataSubscriber.get())
          ? OptionalInt.of(LEGACY_PROTOCOL_VERSION)
          : OptionalInt.empty();
    }
    // The topic can be announced before its value arrives; the default is not a version
    var protocolVersion = protocolVersionSubscriber.getAtomic();
    if (protocolVersion.timestamp == 0) {
      return OptionalInt.empty();
    }
    return OptionalInt.of((int) protocolVersion.value);
  }
}
