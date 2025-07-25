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
#include "frc/geometry/Pose2d.h"

namespace questnav {

struct FrameDataStruct {
    int32_t frame_count;
    double timestamp;
    frc::Pose2d pose2d;
};

} // namespace questnav