/*
 * QUESTNAV
 * https://github.com/QuestNav
 * Copyright (C) 2025 QuestNav
 *
 * This file is part of QuestNav.
 *
 * QuestNav is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * QuestNav is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with QuestNav. If not, see <https://www.gnu.org/licenses/>.
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
