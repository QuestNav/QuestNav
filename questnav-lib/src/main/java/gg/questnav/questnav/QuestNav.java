/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import static edu.wpi.first.units.Units.*;

import edu.wpi.first.math.geometry.Pose2d;
import edu.wpi.first.math.geometry.proto.Pose2dProto;
import edu.wpi.first.math.proto.Geometry2D;
import edu.wpi.first.networktables.*;
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

  /** NetworkTable instance used for communication */
  NetworkTableInstance nt4Instance = NetworkTableInstance.getDefault();

  /** NetworkTable for Quest navigation data */
  NetworkTable questNavTable = nt4Instance.getTable("QuestNav");

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
   * Sets the FRC field relative pose of the Quest. This is the QUESTS POSITION, NOT THE ROBOTS!
   * Make sure you correctly offset back from the center of your robot first!
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
   * Gets the battery percentage of the Quest headset.
   *
   * @return The battery percentage as a Double value
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
   * by checking when the last battery update was received.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  public boolean isConnected() {
    return Seconds.of(Timer.getTimestamp())
        .minus(Microseconds.of(frameData.getLastChange()))
        .lt(Milliseconds.of(50));
  }

  /**
   * Gets the latency of the Quest > Robot Connection
   *
   * @return double indicating the latency of the frameData (the important part)
   */
  public double getLatency() {
    return Seconds.of(Timer.getTimestamp())
        .minus(Microseconds.of(frameData.getLastChange()))
        .in(Milliseconds);
  }

  /**
   * Gets the Quest app's timestamp since start THIS IS NOT THE SAME AS THE TIMESTAMP USED WITH AN
   * ESTIMATOR! See 'getDataTimestamp' instead
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
   * Gets the NT timestamp of when the data was sent. THIS IS THE VALUE YOU USE WHEN ADDING TO AN
   * ESTIMATOR
   *
   * @return The timestamp as a double value
   */
  public double getDataTimestamp() {
    return frameData.getAtomic().serverTime;
  }

  /**
   * Returns the current pose of the Quest on the field.
   * This will only return the field relative pose if you have used {@link #setPose(Pose2d)} at least once.
   *
   * @return Pose2d representing the Quest's location on the field
   */
  public Pose2d getPose() {
    Data.ProtobufQuestNavFrameData latestFrameData = frameData.get();
    if (latestFrameData != null) {
      return pose2dProto.unpack(latestFrameData.getPose2D());
    }
    return Pose2d.kZero; // Return kZero to indicate no data available
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
