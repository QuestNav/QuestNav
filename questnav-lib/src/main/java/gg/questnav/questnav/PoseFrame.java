/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import edu.wpi.first.math.geometry.Pose3d;

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
 *   <li><strong>{@link #dataTimestamp()}:</strong> NetworkTables reception time - use this for pose
 *       estimation
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
 *     QuestNav#setPose(Pose2d)} has been called to establish the field reference frame.
 * @param dataTimestamp The NetworkTables timestamp indicating when this frame data was received by
 *     the robot. This timestamp should be used when adding vision measurements to pose estimators
 *     as it represents when the measurement was available to the robot code. Units: seconds since
 *     robot program start.
 * @param appTimestamp The Quest application's internal timestamp indicating when this frame was
 *     generated. This is primarily useful for debugging timing issues and calculating Quest-side
 *     latency. For pose estimation, use {@link #dataTimestamp()} instead. Units: seconds since
 *     Quest app startup.
 * @param frameCount Sequential frame number from the Quest tracking system. This counter increments
 *     with each tracking frame and can be used to detect dropped frames or measure effective frame
 *     rate. Resets to 0 when the Quest app restarts.
 * @param isTracking Indicates whether the Quest is currently tracking its position
 * @see QuestNav#getAllUnreadPoseFrames()
 * @see QuestNav#setPose(Pose2d)
 * @see edu.wpi.first.math.estimator.PoseEstimator
 * @see edu.wpi.first.math.geometry.Pose2d
 * @since 2025.1.0
 */
public record PoseFrame(
    Pose3d questPose3d,
    double dataTimestamp,
    double appTimestamp,
    int frameCount,
    boolean isTracking) {}
