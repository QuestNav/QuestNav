/*
 * QUESTNAV
   https://github.com/QuestNav
 * Copyright (C) 2025 QuestNav
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the MIT License as published.
 */
#include "gg/questnav/questnav/QuestNav.h"

#include <frc/geometry/proto/Pose2dProto.h>

using namespace gg::questnav::questnav;

QuestNav::QuestNav()
    : nt4_instance_(nt::NetworkTableInstance::GetDefault()),
      quest_nav_table_(nt4_instance_.GetTable("QuestNav")),
      response_(
          quest_nav_table_
              ->GetProtobufTopic<Commands::ProtobufQuestNavCommandResponse>(
                  "response", command_response_proto_)
              .Subscribe({})),
      frame_data_(quest_nav_table_
                      ->GetProtobufTopic<Data::ProtobufQuestNavFrameData>(
                          "frameData", frame_data_proto_)
                      .Subscribe({})),
      device_data_(quest_nav_table_
                       ->GetProtobufTopic<Data::ProtobufQuestNavDeviceData>(
                           "deviceData", device_data_proto_)
                       .Subscribe({})),
      request_(quest_nav_table_
                   ->GetProtobufTopic<Commands::ProtobufQuestNavCommand>(
                       "request", command_proto_)
                   .Publish()) {}

void QuestNav::SetPose(const frc::Pose2d& pose) {
  cached_proto_pose_.Clear();
  wpi::PackProtobuf(&cached_proto_pose_, pose);

  cached_command_request_.Clear();
  cached_pose_reset_payload_.Clear();
  cached_pose_reset_payload_.set_allocated_target_pose(&cached_proto_pose_);

  cached_command_request_.set_type(Commands::QuestNavCommandType::POSE_RESET);
  cached_command_request_.set_command_id(++last_sent_request_id_);
  cached_command_request_.set_allocated_pose_reset_payload(
      &cached_pose_reset_payload_);

  request_.Set(cached_command_request_);
}

int QuestNav::GetBatteryPercent() {
  auto latest_device_data = device_data_.Get();
  if (latest_device_data) {
    return latest_device_data->battery_percent();
  }
  return -1;
}

bool QuestNav::IsTracking() {
  auto latest_device_data = device_data_.Get();
  if (latest_device_data) {
    return latest_device_data->currently_tracking();
  }
  return false;
}

int QuestNav::GetFrameCount() {
  auto latest_frame_data = frame_data_.Get();
  if (latest_frame_data) {
    return latest_frame_data->frame_count();
  }
  return -1;
}

int QuestNav::GetTrackingLostCounter() {
  auto latest_device_data = device_data_.Get();
  if (latest_device_data) {
    return latest_device_data->tracking_lost_counter();
  }
  return -1;
}

bool QuestNav::IsConnected() {
  auto current_time = units::second_t{frc::Timer::GetFPGATimestamp()};
  auto last_change = units::microsecond_t{frame_data_.GetLastChange()};
  return (current_time - last_change) < 50_ms;
}

double QuestNav::GetLatency() {
  auto current_time = units::second_t{frc::Timer::GetFPGATimestamp()};
  auto last_change = units::microsecond_t{frame_data_.GetLastChange()};
  return (current_time - last_change).convert<units::milliseconds>().value();
}

double QuestNav::GetAppTimestamp() {
  auto latest_frame_data = frame_data_.Get();
  if (latest_frame_data) {
    return latest_frame_data->timestamp();
  }
  return -1;
}

double QuestNav::GetDataTimestamp() {
  auto atomic = frame_data_.GetAtomic();
  return atomic.serverTime / 1000000.0;  // Convert microseconds to seconds
}

frc::Pose2d QuestNav::GetPose() {
  auto latest_frame_data = frame_data_.Get();
  if (latest_frame_data) {
    return wpi::UnpackProtobuf<frc::Pose2d>(latest_frame_data->pose2d())
        .value_or(frc::Pose2d{});
  }
  return frc::Pose2d{};
}

void QuestNav::CommandPeriodic() {
  auto latest_command_response = response_.Get();

  if (!latest_command_response ||
      latest_command_response->command_id() != last_sent_request_id_) {
    return;
  }

  if (last_processed_response_id_ != latest_command_response->command_id()) {
    if (!latest_command_response->success()) {
      frc::DriverStation::ReportError("QuestNav command failed!\n" +
                                      latest_command_response->error_message());
    }
    last_processed_response_id_ = latest_command_response->command_id();
  }
}