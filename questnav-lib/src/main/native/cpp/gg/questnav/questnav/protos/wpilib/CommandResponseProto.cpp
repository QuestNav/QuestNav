/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#include <wpi/protobuf/ProtobufCallbacks.h>

#include "gg/questnav/questnav/protos/wpilib/CommandResponseProto.h"

std::optional<questnav_protos_commands_ProtobufQuestNavCommandResponse>
wpi::Protobuf<questnav_protos_commands_ProtobufQuestNavCommandResponse>::Unpack(InputStream& stream) {
  questnav_protos_commands_ProtobufQuestNavCommandResponse msg;
  if (!stream.Decode(msg)) {
    return std::nullopt;
  }
  return msg;
}

bool wpi::Protobuf<questnav_protos_commands_ProtobufQuestNavCommandResponse>::Pack(
    OutputStream& stream, const questnav_protos_commands_ProtobufQuestNavCommandResponse& value) {
  return stream.Encode(value);
}