/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#pragma once

#include <memory>
#include <networktables/NetworkTable.h>
#include <networktables/NetworkTableInstance.h>
#include <networktables/ProtobufTopic.h>
#include <frc/geometry/Pose2d.h>
#include <frc/Timer.h>
#include <wpi/SmallVector.h>

#include "gg/questnav/questnav/protos/generated/commands.npb.h"
#include "gg/questnav/questnav/protos/generated/data.npb.h"
#include "gg/questnav/questnav/protos/wpilib/CommandProto.h"
#include "gg/questnav/questnav/protos/wpilib/CommandResponseProto.h"
#include "gg/questnav/questnav/protos/wpilib/DeviceDataProto.h"
#include "gg/questnav/questnav/protos/wpilib/FrameDataProto.h"
#include <frc/geometry/proto/Pose2dProto.h>

namespace questnav {

/**
 * The QuestNav class provides an interface to communicate with an Oculus/Meta Quest VR headset for
 * robot localization and tracking purposes. It uses NetworkTables to exchange data between the
 * robot and the Quest device.
 */
class QuestNav {
 public:
  /** Creates a new QuestNav implementation */
  QuestNav();

  /**
   * Sets the field-relative pose of the Quest. This is the position of the Quest, not the robot.
   * Make sure you correctly offset back from the center of your robot first.
   *
   * @param pose The field relative position of the Quest
   */
  void SetPose(const frc::Pose2d& pose);

  /**
   * Returns the Quest's battery level (0-100%), or -1 if no data is available
   *
   * @return The battery percentage as an int, or -1 if no data is available
   */
  int GetBatteryPercent();

  /**
   * Gets the current tracking state of the Quest headset.
   *
   * @return Boolean indicating if the Quest is currently tracking (true) or not (false)
   */
  bool IsTracking();

  /**
   * Gets the current frame count from the Quest headset.
   *
   * @return The frame count value
   */
  int GetFrameCount();

  /**
   * Gets the number of tracking lost events since the Quest connected to the robot.
   *
   * @return The tracking lost counter value
   */
  int GetTrackingLostCounter();

  /**
   * Determines if the Quest headset is currently connected to the robot. Connection is determined
   * by how stale the last received frame from the Quest is.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  bool IsConnected();

  /**
   * Gets the latency of the Quest > Robot Connection. Returns the latency between the current time
   * and the last frame data update.
   *
   * @return The latency in milliseconds
   */
  double GetLatency();

  /**
   * Returns the Quest app's uptime timestamp. For integration with a pose estimator, use
   * GetDataTimestamp() instead!
   *
   * @return The timestamp as a double value
   */
  double GetAppTimestamp();

  /**
   * Gets the NT timestamp of when the last frame data was sent. This is the value which should be
   * used with a pose estimator.
   *
   * @return The timestamp as a double value in seconds
   */
  double GetDataTimestamp();

  /**
   * Returns the current pose of the Quest on the field. This will only return the field-relative
   * pose if SetPose(Pose2d) has been called at least once.
   *
   * @return Pose2d representing the Quest's location on the field
   */
  frc::Pose2d GetPose();

  /** Cleans up QuestNav responses after processing on the headset. */
  void CommandPeriodic();

 private:
  /** NetworkTable instance used for communication */
  nt::NetworkTableInstance nt4_instance_;

  /** NetworkTable for Quest navigation data */
  std::shared_ptr<nt::NetworkTable> quest_nav_table_;

  /** Subscriber for command response */
  nt::ProtobufSubscriber<questnav_protos_commands_ProtobufQuestNavCommandResponse> response_;

  /** Subscriber for frame data */
  nt::ProtobufSubscriber<questnav_protos_data_ProtobufQuestNavFrameData> frame_data_;

  /** Subscriber for device data */
  nt::ProtobufSubscriber<questnav_protos_data_ProtobufQuestNavDeviceData> device_data_;

  /** Publisher for command requests */
  nt::ProtobufPublisher<questnav_protos_commands_ProtobufQuestNavCommand> request_;

  /** Cached request to lessen memory pressure */
  wpi::SmallVector<uint8_t, 64> cached_command_request_{};

  /** Cached pose reset request to lessen memory pressure */
  wpi::SmallVector<uint8_t, 64> cached_pose_reset_payload_{};

  /** Cached proto pose (for reset requests) to lessen memory pressure */
  wpi::SmallVector<uint8_t, 64> cached_proto_pose_{};

  /** Last sent request id */
  int last_sent_request_id_ = 0;

  /** Last processed response id */
  int last_processed_response_id_ = 0;

  // Helper functions for unit conversions
  static constexpr double MicrosecondsToSeconds(int64_t microseconds) {
    return microseconds / 1'000'000.0;
  }

  static constexpr double SecondsToMilliseconds(double seconds) {
    return seconds * 1000.0;
  }
};

}  // namespace questnav