/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import static edu.wpi.first.units.Units.Microseconds;
import static edu.wpi.first.units.Units.Milliseconds;
import static edu.wpi.first.units.Units.Seconds;

import edu.wpi.first.math.geometry.Pose3d;
import edu.wpi.first.math.geometry.proto.Pose3dProto;
import edu.wpi.first.math.proto.Geometry3D;
import edu.wpi.first.networktables.NetworkTable;
import edu.wpi.first.networktables.NetworkTableInstance;
import edu.wpi.first.networktables.ProtobufPublisher;
import edu.wpi.first.networktables.ProtobufSubscriber;
import edu.wpi.first.networktables.PubSubOption;
import edu.wpi.first.wpilibj.DriverStation;
import edu.wpi.first.wpilibj.Timer;
import gg.questnav.questnav.protos.generated.Commands;
import gg.questnav.questnav.protos.generated.Data;
import gg.questnav.questnav.protos.wpilib.CommandProto;
import gg.questnav.questnav.protos.wpilib.CommandResponseProto;
import gg.questnav.questnav.protos.wpilib.DeviceDataProto;
import gg.questnav.questnav.protos.wpilib.FrameDataProto;
import java.util.OptionalDouble;
import java.util.OptionalInt;

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
 * </ul>
 *
 * <h2>Basic Usage</h2>
 *
 * <pre>{@code
 * // Create QuestNav instance
 * QuestNav questNav = new QuestNav();
 *
 * // Set initial robot pose (required for field-relative tracking)
 * Pose2d initialPose = new Pose2d(1.0, 2.0, Rotation2d.fromDegrees(90));
 * questNav.setPose(initialPose);
 *
 * // In robot periodic methods
 * public void robotPeriodic() {
 *   questNav.commandPeriodic(); // Process command responses
 *
 *   // Get latest pose data
 *   PoseFrame[] newFrames = questNav.getAllUnreadPoseFrames();
 *   for (PoseFrame frame : newFrames) {
 *     // Use frame.questPose() and frame.dataTimestamp() with pose estimator
 *   }
 *
 *   // Monitor connection and device status
 *   if (questNav.isConnected() && questNav.isTracking()) {
 *     // Quest is connected and tracking - safe to use pose data
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
 *   <li>Call {@link #commandPeriodic()} regularly to process command responses
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
 *   <li>Command failures are reported through DriverStation error logging
 * </ul>
 *
 * @see PoseFrame
 * @see edu.wpi.first.math.geometry.Pose2d
 * @see edu.wpi.first.networktables.NetworkTableInstance
 * @since 2025.1.0
 * @author QuestNav Team
 */
public class QuestNav {

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
              PubSubOption.sendAll(true),
              PubSubOption.pollStorage(20));

  /** Subscriber for frame data */
  private final ProtobufSubscriber<Data.ProtobufQuestNavFrameData> frameDataSubscriber =
      questNavTable
          .getProtobufTopic("frameData", frameDataProto)
          .subscribe(
              Data.ProtobufQuestNavFrameData.newInstance(),
              PubSubOption.periodic(0.01),
              PubSubOption.sendAll(true),
              PubSubOption.pollStorage(20));

  /** Subscriber for device data */
  private final ProtobufSubscriber<Data.ProtobufQuestNavDeviceData> deviceDataSubscriber =
      questNavTable
          .getProtobufTopic("deviceData", deviceDataProto)
          .subscribe(Data.ProtobufQuestNavDeviceData.newInstance());

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
  private final Geometry3D.ProtobufPose3d cachedProtoPose = Geometry3D.ProtobufPose3d.newInstance();

  /** Last sent request id */
  private int lastSentRequestId = 0; // Should be the same on the backend

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
   * <p>The command is sent asynchronously. Monitor command success/failure by calling {@link
   * #commandPeriodic()} regularly, which will log any errors to the DriverStation.
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
   * Pose3d questPose = new Pose3d(robotPose).transformBy(mountingOffset); // Apply your mounting offset
   * questNav.setPose(questPose);
   * }</pre>
   *
   * @param pose The Quest's current field-relative pose in WPILib coordinates (meters for
   *     translation, radians for rotation)
   * @throws IllegalArgumentException if pose contains NaN or infinite values
   * @see #commandPeriodic()
   * @see #isConnected()
   * @see edu.wpi.first.math.geometry.Pose2d
   */
  public void setPose(Pose3d pose) {
    cachedProtoPose.clear(); // Clear instead of creating new
    pose3dProto.pack(cachedProtoPose, pose);
    cachedCommandRequest.clear();
    var requestToSend =
        cachedCommandRequest
            .setType(Commands.QuestNavCommandType.POSE_RESET)
            .setCommandId(++lastSentRequestId)
            .setPoseResetPayload(cachedPoseResetPayload.clear().setTargetPose(cachedProtoPose));

    requestPublisher.set(requestToSend);
  }

  /**
   * Returns the Quest headset's current battery level as a percentage.
   *
   * <p>This method provides real-time battery status information from the Quest device, which is
   * useful for:
   *
   * <ul>
   *   <li>Monitoring device health during matches
   *   <li>Implementing low-battery warnings or behaviors
   *   <li>Planning charging schedules between matches
   *   <li>Triggering graceful shutdown procedures when battery is critical
   * </ul>
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
   * @see #isConnected()
   * @see #getLatency()
   */
  public OptionalInt getBatteryPercent() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceDataSubscriber.get();
    if (latestDeviceData != null) {
      return OptionalInt.of(latestDeviceData.getBatteryPercent());
    }
    return OptionalInt.empty();
  }

  /**
   * Gets the current tracking state of the Quest headset.
   *
   * <p>This method indicates whether the Quest's visual-inertial tracking system is currently
   * functioning and providing reliable pose data. Tracking can be lost due to:
   *
   * <ul>
   *   <li>Poor lighting conditions (too dark or too bright)
   *   <li>Lack of visual features in the environment
   *   <li>Rapid motion or high acceleration
   *   <li>Camera occlusion or obstruction
   *   <li>Hardware issues or overheating
   * </ul>
   *
   * <p><strong>Important:</strong> When tracking is lost, pose data becomes unreliable and should
   * not be used for robot control. Implement fallback localization methods (wheel odometry, vision,
   * etc.) for when Quest tracking is unavailable.
   *
   * <p>To recover tracking:
   *
   * <ul>
   *   <li>Improve lighting conditions
   *   <li>Move to an area with more visual features
   *   <li>Reduce robot motion to allow re-initialization
   *   <li>Clear any obstructions from Quest cameras
   *   <li>Call {@link #setPose(Pose3d)} once tracking recovers
   * </ul>
   *
   * @return {@code true} if the Quest is actively tracking and pose data is reliable, {@code false}
   *     if tracking is lost or no device data is available
   * @see #isConnected()
   * @see #getTrackingLostCounter()
   * @see #getAllUnreadPoseFrames()
   */
  public boolean isTracking() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceDataSubscriber.get();
    if (latestDeviceData != null) {
      return latestDeviceData.getCurrentlyTracking();
    }
    return false; // Return false if no data for failsafe
  }

  /**
   * Gets the current frame count from the Quest headset.
   *
   * @return The frame count value
   */
  public OptionalInt getFrameCount() {
    Data.ProtobufQuestNavFrameData latestFrameData = frameDataSubscriber.get();
    if (latestFrameData != null) {
      return OptionalInt.of(latestFrameData.getFrameCount());
    }
    return OptionalInt.empty();
  }

  /**
   * Gets the number of tracking lost events since the Quest connected to the robot.
   *
   * @return The tracking lost counter value
   */
  public OptionalInt getTrackingLostCounter() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceDataSubscriber.get();
    if (latestDeviceData != null) {
      return OptionalInt.of(latestDeviceData.getTrackingLostCounter());
    }
    return OptionalInt.empty();
  }

  /**
   * Determines if the Quest headset is currently connected to the robot. Connection is determined
   * by how stale the last received frame from the Quest is.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  public boolean isConnected() {
    return Seconds.of(Timer.getTimestamp())
        .minus(Microseconds.of(frameDataSubscriber.getLastChange()))
        .lt(Milliseconds.of(50));
  }

  /**
   * Gets the latency of the Quest > Robot Connection. Returns the latency between the current time
   * and the last frame data update.
   *
   * @return The latency in milliseconds
   */
  public double getLatency() {
    return Seconds.of(Timer.getTimestamp())
        .minus(Microseconds.of(frameDataSubscriber.getLastChange()))
        .in(Milliseconds);
  }

  /**
   * Returns the Quest app's uptime timestamp for debugging and diagnostics.
   *
   * <p><strong>Important:</strong> For integration with a pose estimator, use the timestamp from
   * {@link PoseFrame#dataTimestamp()} instead! This method provides the Quest's internal
   * application timestamp, which is useful for:
   *
   * <ul>
   *   <li>Debugging timing issues between Quest and robot
   *   <li>Calculating Quest-side processing latency
   *   <li>Monitoring Quest application uptime
   *   <li>Correlating with Quest-side logs
   * </ul>
   *
   * <p>The timestamp represents seconds since the Quest application started and is independent of
   * the robot's clock. For pose estimation, always use the NetworkTables timestamp from {@link
   * PoseFrame#dataTimestamp()} which is synchronized with robot time.
   *
   * @return An {@link OptionalDouble} containing the Quest app uptime in seconds, or empty if no
   *     frame data is available
   * @see PoseFrame#dataTimestamp()
   * @see #getAllUnreadPoseFrames()
   */
  public OptionalDouble getAppTimestamp() {
    Data.ProtobufQuestNavFrameData latestFrameData = frameDataSubscriber.get();
    if (latestFrameData != null) {
      return OptionalDouble.of(latestFrameData.getTimestamp());
    }
    return OptionalDouble.empty();
  }

  /**
   * Retrieves all new pose frames received from the Quest since the last call to this method.
   *
   * <p>This is the primary method for integrating QuestNav with FRC pose estimation systems. It
   * returns an array of {@link PoseFrame} objects containing pose data and timestamps that can be
   * fed directly into a {@link edu.wpi.first.math.estimator.PoseEstimator}.
   *
   * <p>Each frame contains:
   *
   * <ul>
   *   <li><strong>Pose data:</strong> Robot position and orientation in field coordinates
   *   <li><strong>NetworkTables timestamp:</strong> When the data was received (use this for pose
   *       estimation)
   *   <li><strong>App timestamp:</strong> Quest internal timestamp (for debugging only)
   *   <li><strong>Frame count:</strong> Sequential frame number for detecting drops
   * </ul>
   *
   * <p><strong>Important:</strong> This method consumes the frame queue, so each frame is only
   * returned once. Call this method regularly (every robot loop) to avoid missing frames.
   *
   * <h4>Integration with Pose Estimator:</h4>
   *
   * <pre>{@code
   * // In robotPeriodic() or subsystem periodic()
   * PoseFrame[] newFrames = questNav.getAllUnreadPoseFrames();
   * for (PoseFrame frame : newFrames) {
   *   if (questNav.isTracking() && questNav.isConnected()) {
   *     // Add vision measurement to pose estimator
   *     poseEstimator.addVisionMeasurement(
   *       frame.questPose(),           // Measured pose
   *       frame.dataTimestamp(),       // When measurement was taken
   *       VecBuilder.fill(0.1, 0.1, 0.05)  // Standard deviations (tune these)
   *     );
   *   }
   * }
   * }</pre>
   *
   * <p>Performance notes:
   *
   * <ul>
   *   <li>Returns a new array each call - consider caching if called multiple times per loop
   *   <li>Frame rate is exactly 100 Hz (every 10 milliseconds)
   *   <li>Empty array returned when no new frames are available
   * </ul>
   *
   * @return Array of new {@link PoseFrame} objects received since the last call. Empty array if no
   *     new frames are available or Quest is disconnected.
   * @see PoseFrame
   * @see #isTracking()
   * @see #isConnected()
   * @see edu.wpi.first.math.estimator.PoseEstimator
   */
  public PoseFrame[] getAllUnreadPoseFrames() {
    var frameDataArray = frameDataSubscriber.readQueue();
    var result = new PoseFrame[frameDataArray.length];
    for (int i = 0; i < result.length; i++) {
      var frameData = frameDataArray[i];
      result[i] =
          new PoseFrame(
              pose3dProto.unpack(frameData.value.getPose3D()),
              Microseconds.of(frameData.serverTime).in(Seconds),
              frameData.value.getTimestamp(),
              frameData.value.getFrameCount(),
              frameData.value.getIsTracking());
    }
    return result;
  }

  /**
   * Processes command responses from the Quest headset and handles any errors.
   *
   * <p>This method must be called regularly (typically in {@code robotPeriodic()}) to:
   *
   * <ul>
   *   <li>Process responses to commands sent via {@link #setPose(Pose3d)}
   *   <li>Log command failures to the DriverStation for debugging
   *   <li>Maintain proper command/response synchronization
   *   <li>Prevent command response queue overflow
   * </ul>
   *
   * <p>The method automatically handles:
   *
   * <ul>
   *   <li><strong>Success responses:</strong> Silently acknowledged
   *   <li><strong>Error responses:</strong> Logged to DriverStation with error details
   *   <li><strong>Duplicate responses:</strong> Ignored to prevent spam
   *   <li><strong>Out-of-order responses:</strong> Handled gracefully
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
   *
   *     // Other periodic tasks...
   *   }
   * }
   * }</pre>
   *
   * <p><strong>Performance:</strong> This method is lightweight and safe to call every robot loop
   * (20ms). It only processes new responses and exits quickly when none are available.
   *
   * @see #setPose(Pose3d)
   * @see edu.wpi.first.wpilibj.DriverStation#reportError(String, boolean)
   */
  public void commandPeriodic() {
    Commands.ProtobufQuestNavCommandResponse[] responses = responseSubscriber.readQueueValues();

    for (Commands.ProtobufQuestNavCommandResponse response : responses) {
      if (!response.getSuccess()) {
        DriverStation.reportError("QuestNav command failed!\n" + response.getErrorMessage(), false);
      }
    }
  }
}
