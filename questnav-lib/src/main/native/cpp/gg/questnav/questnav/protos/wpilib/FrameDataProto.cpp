/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#include <wpi/protobuf/ProtobufCallbacks.h>

#include "gg/questnav/questnav/protos/wpilib/FrameDataProto.h"

std::optional<questnav_protos_data_ProtobufQuestNavFrameData >
wpi::Protobuf<questnav_protos_data_ProtobufQuestNavFrameData >::Unpack(InputStream& stream) {
  questnav_protos_data_ProtobufQuestNavFrameData  msg;
  if (!stream.Decode(msg)) {
    return std::nullopt;
  }
  return msg;
}

bool wpi::Protobuf<questnav_protos_data_ProtobufQuestNavFrameData >::Pack(
    OutputStream& stream, const questnav_protos_data_ProtobufQuestNavFrameData & value) {
  return stream.Encode(value);
}