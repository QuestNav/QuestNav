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
#include "gg/questnav/questnav/protos/generated/data.npb.h"
#include "gg/questnav/questnav/struct/QuestNavFrameDataStruct.h"

template <>
struct wpi::Protobuf<questnav::FrameDataStruct> {
  using MessageStruct = questnav_protos_data_ProtobufQuestNavFrameData;
  using InputStream = wpi::ProtoInputStream<questnav::FrameDataStruct>;
  using OutputStream = wpi::ProtoOutputStream<questnav::FrameDataStruct>;

  static std::optional<questnav::FrameDataStruct> Unpack(InputStream& stream);
  static bool Pack(OutputStream& stream, const questnav::FrameDataStruct& value);
};