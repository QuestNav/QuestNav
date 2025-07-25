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
#include "gg/questnav/questnav/struct/CommandResponseStruct.h"
#include "gg/questnav/questnav/protos/generated/commands.npb.h"

template <>
struct wpi::Protobuf<questnav::CommandResponseStruct> {
  using MessageStruct = questnav_protos_commands_ProtobufQuestNavCommandResponse;
  using InputStream = wpi::ProtoInputStream<questnav::CommandResponseStruct>;
  using OutputStream = wpi::ProtoOutputStream<questnav::CommandResponseStruct>;

  static std::optional<questnav::CommandResponseStruct> Unpack(InputStream& stream);
  static bool Pack(OutputStream& stream, const questnav::CommandResponseStruct& value);
};