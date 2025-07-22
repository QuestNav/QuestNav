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
#include "gg/questnav/questnav/protos/generated/data.pb.h"

namespace gg {
namespace questnav {
namespace questnav {
namespace protos {
namespace wpilib {

template <>
struct wpi::Protobuf<questnav_protos_data_ProtobufQuestNavFrameData> {
  static std::optional<questnav_protos_data_ProtobufQuestNavFrameData> Unpack(
      std::span<const uint8_t> data) {
    questnav_protos_data_ProtobufQuestNavFrameData msg{};
    pb_istream_t stream = pb_istream_from_buffer(data.data(), data.size());
    if (!pb_decode(&stream, &questnav_protos_data_ProtobufQuestNavFrameData_msg, &msg)) {
      return {};
    }
    return msg;
  }

  static bool Pack(std::vector<uint8_t>& data,
                   const questnav_protos_data_ProtobufQuestNavFrameData& value) {
    pb_ostream_t sizing_stream{};
    if (!pb_encode(&sizing_stream, &questnav_protos_data_ProtobufQuestNavFrameData_msg, &value)) {
      return false;
    }

    data.resize(sizing_stream.bytes_written);
    pb_ostream_t stream = pb_ostream_from_buffer(data.data(), data.size());
    return pb_encode(&stream, &questnav_protos_data_ProtobufQuestNavFrameData_msg, &value);
  }

  static constexpr std::string_view GetTypeString() {
    return "proto:questnav_protos_data_ProtobufQuestNavFrameData";
  }
};

using FrameDataProto = wpi::Protobuf<questnav_protos_data_ProtobufQuestNavFrameData>;

}  // namespace wpilib
}  // namespace protos
}  // namespace questnav
}  // namespace questnav
}  // namespace gg