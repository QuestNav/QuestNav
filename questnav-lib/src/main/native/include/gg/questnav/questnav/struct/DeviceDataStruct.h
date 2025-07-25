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

namespace questnav {

    struct DeviceDataStruct {
        int32_t tracking_lost_counter;
        bool currently_tracking;
        int32_t battery_percent;
    };

} // namespace questnav