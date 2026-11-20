/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import gg.questnav.questnav.protos.wpilib.FrameDataProto;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.util.protobuf.ProtobufSerializable;

/**
 * Represents a single frame of pose tracking data received from the Quest headset.
 *
 * <p>This record encapsulates all the information needed to integrate Quest tracking data with FRC
 * pose estimation systems. Each frame represents a single tracking measurement from the Quest's
 * visual-inertial odometry system.
 *
 * <h2>Usage with Pose Estimators</h2>
 *
 * <p>This record is designed to work seamlessly with WPILib's pose estimation framework:
 *
 * <pre>{@code
 * PoseFrame[] frames = questNav.getAllUnreadPoseFrames();
 * for (PoseFrame frame : frames) {
 *   if (questNav.isTracking()) {
 *     poseEstimator.addVisionMeasurement(
 *       frame.questPose(),      // Use the pose measurement
 *       frame.dataTimestamp(),  // Use the NetworkTables timestamp
 *       standardDeviations      // Your measurement uncertainty
 *     );
 *   }
 * }
 * }</pre>
 *
 * <h2>Timestamp Usage</h2>
 *
 * <p>Two timestamps are provided for different use cases:
 *
 * <ul>
 *   <li><strong>{@link #dataTimestamp()}:</strong> When the pose was captured, in NetworkTables
 *       server time - use this for pose estimation
 *   <li><strong>{@link #appTimestamp()}:</strong> Quest internal time - use only for
 *       debugging/diagnostics
 * </ul>
 *
 * <h2>Coordinate System</h2>
 *
 * <p>The pose data follows WPILib field coordinate conventions:
 *
 * <ul>
 *   <li>X-axis: Forward (towards opposing alliance)
 *   <li>Y-axis: Left (when facing forward)
 *   <li>Rotation: Counter-clockwise positive
 *   <li>Units: Meters for translation, radians for rotation
 * </ul>
 *
 * @param questPose3d The robot's pose on the field as measured by the Quest tracking system. This
 *     will only provide meaningful field-relative coordinates after {@link
 *     QuestNav#setPose(Pose3d)} has been called to establish the field reference frame.
 * @param dataTimestamp When the Quest captured this frame, in the NetworkTables server time base
 *     (on the robot, the same time base as {@link org.wpilib.system.Timer#getTimestamp()}). Use
 *     this timestamp when adding vision measurements to pose estimators. If the QuestNav app does
 *     not send a capture time, this is the time the robot received the frame instead. Units:
 *     seconds.
 * @param appTimestamp The Quest application's internal timestamp indicating when this frame was
 *     generated. This is primarily useful for debugging timing issues and calculating Quest-side
 *     latency. For pose estimation, use {@link #dataTimestamp()} instead. Units: seconds since
 *     Quest app startup.
 * @param frameCount Sequential frame number from the Quest tracking system. This counter increments
 *     with each tracking frame and can be used to detect dropped frames or measure effective frame
 *     rate. Resets to 0 when the Quest app restarts.
 * @param isTracking Indicates whether the Quest is currently tracking its position
 * @param alignmentState What {@code questPose3d} is referenced to. {@link AlignmentState#UNKNOWN}
 *     when the QuestNav app is too old to send it.
 * @param resetEpoch Counter that changes whenever the Quest's pose is re-placed instead of
 *     following its motion: a pose reset, an AprilTag alignment or re-alignment, or a tracking
 *     relocalization. Across a change, the change in pose is not motion. Within one epoch the pose
 *     follows the Quest's motion plus filtered AprilTag corrections, which are usually small but
 *     can step it by up to 2 m in one frame (for example after a tracking outage). Compare it for
 *     equality only: it starts at a random value when the app starts, so a restarted app is very
 *     unlikely to repeat the last value, can be negative, and wraps around. 0 when the QuestNav app
 *     is too old to send it.
 * @see QuestNav#getAllUnreadPoseFrames()
 * @see QuestNav#setPose(Pose3d)
 * @see org.wpilib.math.estimator.PoseEstimator
 * @see org.wpilib.math.geometry.Pose2d
 * @since 2025.1.0
 */
public record PoseFrame(
    Pose3d questPose3d,
    double dataTimestamp,
    double appTimestamp,
    int frameCount,
    boolean isTracking,
    AlignmentState alignmentState,
    int resetEpoch)
    implements ProtobufSerializable {
  /** Protobuf serializer for {@link PoseFrame} */
  public static final FrameDataProto proto = new FrameDataProto();

  /** A null alignment state is stored as {@link AlignmentState#UNKNOWN}. */
  public PoseFrame {
    if (alignmentState == null) {
      alignmentState = AlignmentState.UNKNOWN;
    }
  }

  /**
   * Creates a frame without alignment information, as from a QuestNav app that predates it: the
   * alignment state is {@link AlignmentState#UNKNOWN} and the reset epoch 0. This is the canonical
   * constructor from before those were added, so existing code keeps compiling.
   *
   * @param questPose3d The Quest's pose on the field
   * @param dataTimestamp When the Quest captured this frame, in NetworkTables server time, seconds
   * @param appTimestamp The Quest app's internal timestamp, seconds since app startup
   * @param frameCount Sequential frame number from the Quest tracking system
   * @param isTracking Whether the Quest is currently tracking its position
   */
  public PoseFrame(
      Pose3d questPose3d,
      double dataTimestamp,
      double appTimestamp,
      int frameCount,
      boolean isTracking) {
    this(
        questPose3d,
        dataTimestamp,
        appTimestamp,
        frameCount,
        isTracking,
        AlignmentState.UNKNOWN,
        0);
  }
}
