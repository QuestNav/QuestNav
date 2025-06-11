/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav.protos.wpilib;

import edu.wpi.first.util.protobuf.Protobuf;
import gg.questnav.questnav.protos.generated.Commands;
import us.hebi.quickbuf.Descriptors;

/** WPILib Protobuf layer for CommandResponse Protobuf */
public class CommandResponseProto
    implements Protobuf<
        Commands.ProtobufQuestNavCommandResponse, Commands.ProtobufQuestNavCommandResponse> {
  @Override
  public Class<Commands.ProtobufQuestNavCommandResponse> getTypeClass() {
    return Commands.ProtobufQuestNavCommandResponse.class;
  }

  @Override
  public Descriptors.Descriptor getDescriptor() {
    return Commands.ProtobufQuestNavCommandResponse.getDescriptor();
  }

  @Override
  public Commands.ProtobufQuestNavCommandResponse createMessage() {
    return Commands.ProtobufQuestNavCommandResponse.newInstance();
  }

  @Override
  public Commands.ProtobufQuestNavCommandResponse unpack(
      Commands.ProtobufQuestNavCommandResponse msg) {
    return msg.clone();
  }

  @Override
  public void pack(
      Commands.ProtobufQuestNavCommandResponse msg,
      Commands.ProtobufQuestNavCommandResponse value) {
    msg.copyFrom(value);
  }

  @Override
  public boolean isImmutable() {
    return true;
  }
}
