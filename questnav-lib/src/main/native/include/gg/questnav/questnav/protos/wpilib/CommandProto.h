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
#include "gg/questnav/questnav/struct/CommandStruct.h"
#include "gg/questnav/questnav/protos/generated/commands.npb.h"

template <>
struct wpi::Protobuf<questnav::CommandStruct> {
  using MessageStruct = questnav_protos_commands_ProtobufQuestNavCommand;
  using InputStream = wpi::ProtoInputStream<questnav::CommandStruct>;
  using OutputStream = wpi::ProtoOutputStream<questnav::CommandStruct>;

  static std::optional<questnav::CommandStruct> Unpack(InputStream& stream);
  static bool Pack(OutputStream& stream, const questnav::CommandStruct& value);
};