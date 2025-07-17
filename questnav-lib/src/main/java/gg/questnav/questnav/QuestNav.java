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

import edu.wpi.first.math.geometry.Pose2d;
import edu.wpi.first.math.geometry.proto.Pose2dProto;
import edu.wpi.first.math.proto.Geometry2D;
import edu.wpi.first.networktables.NetworkTable;
import edu.wpi.first.networktables.NetworkTableInstance;
import edu.wpi.first.networktables.ProtobufPublisher;
import edu.wpi.first.networktables.ProtobufSubscriber;
import edu.wpi.first.wpilibj.DriverStation;
import edu.wpi.first.wpilibj.Timer;
import gg.questnav.questnav.protos.generated.Commands;
import gg.questnav.questnav.protos.generated.Data;
import gg.questnav.questnav.protos.wpilib.CommandProto;
import gg.questnav.questnav.protos.wpilib.CommandResponseProto;
import gg.questnav.questnav.protos.wpilib.DeviceDataProto;
import gg.questnav.questnav.protos.wpilib.FrameDataProto;

/**
 * The QuestNav class provides an interface to communicate with an Oculus/Meta Quest VR headset for
 * robot localization and tracking purposes. It uses NetworkTables to exchange data between the
 * robot and the Quest device.
 */
public class QuestNav {

  /**
   * A frame of data from the QuestNav.
   *
   * @param questPose The current pose of the Quest on the field. This will only return the
   *     field-relative pose if {@link #setPose(Pose2d)} has been called at least once.
   * @param dataTimestamp The NT timestamp of when the last frame data was sent. This is the value
   *     which should be used with a pose estimator.
   */
  public static record PoseFrame(Pose2d questPose, double dataTimestamp) {}

  /** NetworkTable instance used for communication */
  private NetworkTableInstance nt4Instance = NetworkTableInstance.getDefault();

  /** NetworkTable for Quest navigation data */
  private NetworkTable questNavTable = nt4Instance.getTable("QuestNav");

  /** Protobuf instance for CommandResponse */
  private final CommandResponseProto commandResponseProto = new CommandResponseProto();

  /** Protobuf instance for Command */
  private final CommandProto commandProto = new CommandProto();

  /** Protobuf instance for Pose2d */
  private final Pose2dProto pose2dProto = new Pose2dProto();

  /** Protobuf instance for device data */
  private final DeviceDataProto deviceDataProto = new DeviceDataProto();

  /** Protobuf instance for frame data */
  private final FrameDataProto frameDataProto = new FrameDataProto();

  /** Subscriber for command response */
  private final ProtobufSubscriber<Commands.ProtobufQuestNavCommandResponse> response =
      questNavTable
          .getProtobufTopic("response", commandResponseProto)
          .subscribe(Commands.ProtobufQuestNavCommandResponse.newInstance());

  /** Subscriber for frame data */
  private final ProtobufSubscriber<Data.ProtobufQuestNavFrameData> frameData =
      questNavTable
          .getProtobufTopic("frameData", frameDataProto)
          .subscribe(Data.ProtobufQuestNavFrameData.newInstance());

  /** Subscriber for device data */
  private final ProtobufSubscriber<Data.ProtobufQuestNavDeviceData> deviceData =
      questNavTable
          .getProtobufTopic("deviceData", deviceDataProto)
          .subscribe(Data.ProtobufQuestNavDeviceData.newInstance());

  /** Publisher for command requests */
  private final ProtobufPublisher<Commands.ProtobufQuestNavCommand> request =
      questNavTable.getProtobufTopic("request", commandProto).publish();

  /** Cached request to lessen GC pressure */
  private final Commands.ProtobufQuestNavCommand cachedCommandRequest =
      Commands.ProtobufQuestNavCommand.newInstance();

  /** Cached pose reset request to lessen GC pressure */
  private final Commands.ProtobufQuestNavPoseResetPayload cachedPoseResetPayload =
      Commands.ProtobufQuestNavPoseResetPayload.newInstance();

  /** Cached proto pose (for reset requests) to lessen GC pressure */
  private final Geometry2D.ProtobufPose2d cachedProtoPose = Geometry2D.ProtobufPose2d.newInstance();

  /** Last sent request id */
  private int lastSentRequestId = 0; // Should be the same on the backend

  /** Last processed response id */
  private int lastProcessedResponseId = 0; // Should be the same on the backend

  /** Creates a new QuestNav implementation */
  public QuestNav() {}

  /**
   * Sets the field-relative pose of the Quest. This is the position of the Quest, not the robot.
   * Make sure you correctly offset back from the center of your robot first.
   *
   * @param pose The field relative position of the Quest
   */
  public void setPose(Pose2d pose) {
    cachedProtoPose.clear(); // Clear instead of creating new
    pose2dProto.pack(cachedProtoPose, pose);
    cachedCommandRequest.clear();
    var requestToSend =
        cachedCommandRequest
            .setType(Commands.QuestNavCommandType.POSE_RESET)
            .setCommandId(++lastSentRequestId)
            .setPoseResetPayload(cachedPoseResetPayload.clear().setTargetPose(cachedProtoPose));

    request.set(requestToSend);
  }

  /**
   * Returns the Quest's battery level (0-100%), or -1 if no data is available
   *
   * @return The battery percentage as an int, or -1 if no data is available
   */
  public int getBatteryPercent() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceData.get();
    if (latestDeviceData != null) {
      return latestDeviceData.getBatteryPercent();
    }
    return -1; // Return -1 to indicate no data available
  }

  /**
   * Gets the current tracking state of the Quest headset.
   *
   * @return Boolean indicating if the Quest is currently tracking (true) or not (false)
   */
  public boolean isTracking() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceData.get();
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
  public int getFrameCount() {
    Data.ProtobufQuestNavFrameData latestFrameData = frameData.get();
    if (latestFrameData != null) {
      return latestFrameData.getFrameCount();
    }
    return -1; // Return -1 to indicate no data available
  }

  /**
   * Gets the number of tracking lost events since the Quest connected to the robot.
   *
   * @return The tracking lost counter value
   */
  public int getTrackingLostCounter() {
    Data.ProtobufQuestNavDeviceData latestDeviceData = deviceData.get();
    if (latestDeviceData != null) {
      return latestDeviceData.getTrackingLostCounter();
    }
    return -1; // Return -1 to indicate no data available
  }

  /**
   * Determines if the Quest headset is currently connected to the robot. Connection is determined
   * by how stale the last received frame from the Quest is.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  public boolean isConnected() {
    return Seconds.of(Timer.getTimestamp())
        .minus(Microseconds.of(frameData.getLastChange()))
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
        .minus(Microseconds.of(frameData.getLastChange()))
        .in(Milliseconds);
  }

  /**
   * Returns the Quest app's uptime timestamp. For integration with a pose estimator, use the
   * timestamp from {@link #getPoseFrame()} instead!
   *
   * @return The timestamp as a double value
   */
  public double getAppTimestamp() {
    Data.ProtobufQuestNavFrameData latestFrameData = frameData.get();
    if (latestFrameData != null) {
      return latestFrameData.getTimestamp();
    }
    return -1; // Return -1 to indicate no data available
  }

  /**
   * Returns the last frame of pose data from the Quest.
   *
   * @return returns the latest frame of pose data
   */
  public PoseFrame getPoseFrame() {
    var latestFrameData = frameData.getAtomic();
    if (latestFrameData != null) {
      return new PoseFrame(
          pose2dProto.unpack(latestFrameData.value.getPose2D()),
          Microseconds.of(latestFrameData.serverTime).in(Seconds));
    }
    return new PoseFrame(Pose2d.kZero, -1);
  }

  /** Cleans up QuestNav responses after processing on the headset. */
  public void commandPeriodic() {
    Commands.ProtobufQuestNavCommandResponse latestCommandResponse = response.get();

    // if we don't have data or for some reason the response we got isn't for the command we sent,
    // skip for this loop
    if (latestCommandResponse == null || latestCommandResponse.getCommandId() != lastSentRequestId)
      return;

    if (lastProcessedResponseId != latestCommandResponse.getCommandId()) {

      if (!latestCommandResponse.getSuccess()) {
        DriverStation.reportError(
            "QuestNav command failed!\n" + latestCommandResponse.getErrorMessage(), false);
      }
      // don't double process
      lastProcessedResponseId = latestCommandResponse.getCommandId();
    }
  }
}
