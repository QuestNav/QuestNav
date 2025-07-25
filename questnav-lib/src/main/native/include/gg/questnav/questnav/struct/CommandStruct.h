/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#pragma once

#include <cstdint>
#include <variant>
#include "frc/geometry/Pose2d.h"

namespace questnav {

    enum CommandType {
      COMMAND_TYPE_UNSPECIFIED,
      POSE_RESET
      // Future commands can be added here
    };

    struct PoseResetPayload {
        frc::Pose2d target_pose;
      };

    struct CommandStruct {
        CommandType type;
        uint32_t command_id;
        std::variant<PoseResetPayload> payload;
    };

} // namespace questnav