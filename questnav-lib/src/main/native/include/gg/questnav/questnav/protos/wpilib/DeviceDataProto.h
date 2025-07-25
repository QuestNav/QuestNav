/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#pragma once

#include <wpi/protobuf/Protobuf.h>
#include "gg/questnav/questnav/struct/DeviceDataStruct.h"
#include "gg/questnav/questnav/protos/generated/data.npb.h"

template <>
struct wpi::Protobuf<questnav::DeviceDataStruct> {
  using MessageStruct = questnav_protos_data_ProtobufQuestNavDeviceData;
  using InputStream = wpi::ProtoInputStream<questnav::DeviceDataStruct>;
  using OutputStream = wpi::ProtoOutputStream<questnav::DeviceDataStruct>;

  static std::optional<questnav::DeviceDataStruct> Unpack(InputStream& stream);
  static bool Pack(OutputStream& stream, const questnav::DeviceDataStruct& value);
};