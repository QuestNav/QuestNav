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
#include <string>

namespace questnav {

    struct CommandResponseStruct {
        uint32_t tracking_lost_counter;
        bool success;
        std::string battery_percent;
    };

} // namespace questnav