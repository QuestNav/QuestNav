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

/** WPILib Protobuf layer for DeviceData Protobuf */
public class DeviceDataProto
    implements Protobuf<Data.ProtobufQuestNavDeviceData, Data.ProtobufQuestNavDeviceData> {
  @Override
  public Class<Data.ProtobufQuestNavDeviceData> getTypeClass() {
    return Data.ProtobufQuestNavDeviceData.class;
  }

  @Override
  public Descriptors.Descriptor getDescriptor() {
    return Data.ProtobufQuestNavDeviceData.getDescriptor();
  }

  @Override
  public Data.ProtobufQuestNavDeviceData createMessage() {
    return Data.ProtobufQuestNavDeviceData.newInstance();
  }

  @Override
  public Data.ProtobufQuestNavDeviceData unpack(Data.ProtobufQuestNavDeviceData msg) {
    return msg.clone();
  }

  @Override
  public void pack(Data.ProtobufQuestNavDeviceData msg, Data.ProtobufQuestNavDeviceData value) {
    msg.copyFrom(value);
  }
}
