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

/** WPILib Protobuf layer for Commands Protobuf */
public class CommandProto
    implements Protobuf<Commands.ProtobufQuestNavCommand, Commands.ProtobufQuestNavCommand> {
  @Override
  public Class<Commands.ProtobufQuestNavCommand> getTypeClass() {
    return Commands.ProtobufQuestNavCommand.class;
  }

  @Override
  public Descriptors.Descriptor getDescriptor() {
    return Commands.ProtobufQuestNavCommand.getDescriptor();
  }

  @Override
  public Commands.ProtobufQuestNavCommand createMessage() {
    return Commands.ProtobufQuestNavCommand.newInstance();
  }

  @Override
  public Commands.ProtobufQuestNavCommand unpack(Commands.ProtobufQuestNavCommand msg) {
    return msg.clone();
  }

  @Override
  public void pack(Commands.ProtobufQuestNavCommand msg, Commands.ProtobufQuestNavCommand value) {
    msg.copyFrom(value);
  }

  @Override
  public boolean isImmutable() {
    return true;
  }
}
