/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
#include <wpi/protobuf/ProtobufCallbacks.h>

#include "gg/questnav/questnav/protos/wpilib/DeviceDataProto.h"

std::optional<questnav_protos_data_ProtobufQuestNavDeviceData >
wpi::Protobuf<questnav_protos_data_ProtobufQuestNavDeviceData >::Unpack(InputStream& stream) {
  questnav_protos_data_ProtobufQuestNavDeviceData  msg;
  if (!stream.Decode(msg)) {
    return std::nullopt;
  }
  return msg;
}

bool wpi::Protobuf<questnav_protos_data_ProtobufQuestNavDeviceData >::Pack(
    OutputStream& stream, const questnav_protos_data_ProtobufQuestNavDeviceData& value) {
  return stream.Encode(value);
}