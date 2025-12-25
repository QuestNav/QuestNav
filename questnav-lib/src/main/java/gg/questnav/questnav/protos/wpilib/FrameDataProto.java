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
import gg.questnav.questnav.protos.generated.Data;
import us.hebi.quickbuf.Descriptors;

/** WPILib Protobuf layer for FrameData Protobuf */
public class FrameDataProto
    implements Protobuf<Data.ProtobufQuestNavFrameData, Data.ProtobufQuestNavFrameData> {
  @Override
  public Class<Data.ProtobufQuestNavFrameData> getTypeClass() {
    return Data.ProtobufQuestNavFrameData.class;
  }

  @Override
  public Descriptors.Descriptor getDescriptor() {
    return Data.ProtobufQuestNavFrameData.getDescriptor();
  }

  @Override
  public Data.ProtobufQuestNavFrameData createMessage() {
    return Data.ProtobufQuestNavFrameData.newInstance();
  }

  @Override
  public Data.ProtobufQuestNavFrameData unpack(Data.ProtobufQuestNavFrameData msg) {
    return msg.clone();
  }

  @Override
  public void pack(Data.ProtobufQuestNavFrameData msg, Data.ProtobufQuestNavFrameData value) {
    msg.copyFrom(value);
  }
}
