/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#include "gg/questnav/questnav/QuestNav.h"
#include <frc/DriverStation.h>
#include <fmt/core.h>

namespace questnav {

QuestNav::QuestNav()
    : nt4_instance_(nt::NetworkTableInstance::GetDefault()),
      quest_nav_table_(nt4_instance_.GetTable("QuestNav")),
      response_(quest_nav_table_->GetProtobufTopic<questnav_protos_commands_ProtobufQuestNavCommandResponse>("response").Subscribe({})),
      frame_data_(quest_nav_table_->GetProtobufTopic<questnav_protos_data_ProtobufQuestNavFrameData>("frameData").Subscribe({})),
      device_data_(quest_nav_table_->GetProtobufTopic<questnav_protos_data_ProtobufQuestNavDeviceData>("deviceData").Subscribe({})),
      request_(quest_nav_table_->GetProtobufTopic<questnav_protos_commands_ProtobufQuestNavCommand>("request").Publish()) {
}

void QuestNav::SetPose(const frc::Pose2d& pose) {
    // Initialize the nanopb structures
    cached_command_request_ = questnav_protos_commands_ProtobufQuestNavCommand_init_zero;
    cached_pose_reset_payload_ = questnav_protos_commands_ProtobufQuestNavPoseResetPayload_init_zero;
    cached_proto_pose_ = wpi_proto_ProtobufPose2d_init_zero;

    // Pack the pose into the proto - need to manually set fields for nanopb
    cached_proto_pose_::Pack

    // Set up the pose reset payload
    cached_pose_reset_payload_.target_pose = cached_proto_pose_;

    // Set up the command
    cached_command_request_.type = questnav_protos_commands_QuestNavCommandType_POSE_RESET;
    cached_command_request_.command_id = ++last_sent_request_id_;
    cached_command_request_.payload.pose_reset_payload = cached_pose_reset_payload_;

    request_.Set(cached_command_request_);
}

int QuestNav::GetBatteryPercent() {
    auto latest_device_data = device_data_.Get();
    if (latest_device_data != nullptr) {
        return latest_device_data.battery_percent;
    }
    return -1; // Return -1 to indicate no data available
}

bool QuestNav::IsTracking() {
    auto latest_device_data = device_data_.Get();
    if (latest_device_data != nullptr) {
        return latest_device_data.currently_tracking;
    }
    return false; // Return false if no data for failsafe
}

int QuestNav::GetFrameCount() {
    auto latest_frame_data = frame_data_.Get();
    if (latest_frame_data != nullptr) {
        return latest_frame_data.frame_count;
    }
    return -1; // Return -1 to indicate no data available
}

int QuestNav::GetTrackingLostCounter() {
    auto latest_device_data = device_data_.Get();
    if (latest_device_data != nullptr) {
        return latest_device_data.tracking_lost_counter;
    }
    return -1; // Return -1 to indicate no data available
}

bool QuestNav::IsConnected() {
    auto atomic_data = frame_data_.GetAtomic();
    double current_time = frc::Timer::GetTimestamp().value();
    double last_change_time = MicrosecondsToSeconds(atomic_data.time);
    return (current_time - last_change_time) < 0.050; // 50 milliseconds
}

double QuestNav::GetLatency() {
    auto atomic_data = frame_data_.GetAtomic();
    double current_time = frc::Timer::GetTimestamp().value();
    double last_change_time = MicrosecondsToSeconds(atomic_data.time);
    return SecondsToMilliseconds(current_time - last_change_time);
}

double QuestNav::GetAppTimestamp() {
    auto latest_frame_data = frame_data_.Get();
    if (latest_frame_data != nullptr) {
        return latest_frame_data.timestamp;
    }
    return -1; // Return -1 to indicate no data available
}

double QuestNav::GetDataTimestamp() {
    auto atomic_data = frame_data_.GetAtomic();
    return MicrosecondsToSeconds(atomic_data.serverTime);
}

frc::Pose2d QuestNav::GetPose() {
    auto latest_frame_data = frame_data_.Get();
    if (latest_frame_data != nullptr) {
        const auto& pose_proto = latest_frame_data.pose2d;
        return frc::Pose2d{
            units::meter_t{pose_proto.x},
            units::meter_t{pose_proto.y},
            frc::Rotation2d{units::radian_t{pose_proto.rotation.radians}}
        };
    }
    return frc::Pose2d{}; // Return zero pose to indicate no data available
}

void QuestNav::CommandPeriodic() {
    auto latest_command_response = response_.Get();

    // if we don't have data or for some reason the response we got isn't for the command we sent,
    // skip for this loop
    if (!latest_command_response != nullptr ||
        latest_command_response.command_id != last_sent_request_id_) {
        return;
    }

    if (last_processed_response_id_ != latest_command_response.command_id) {
        if (!latest_command_response.success) {
            frc::DriverStation::ReportError(
                fmt::format("QuestNav command failed!\n{}",
                           latest_command_response.error_message));
        }
        // don't double process
        last_processed_response_id_ = latest_command_response.command_id;
    }
}

}  // namespace questnav