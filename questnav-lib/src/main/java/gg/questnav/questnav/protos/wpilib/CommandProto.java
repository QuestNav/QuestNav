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

/**
 * WPILib Protobuf serialization adapter for QuestNav command messages.
 *
 * <p>This class provides the necessary integration between QuestNav's protocol buffer command
 * definitions and WPILib's NetworkTables protobuf system. It handles the serialization and
 * deserialization of command messages sent from robot code to the Quest headset.
 *
 * <p>This adapter is used internally by the {@link gg.questnav.questnav.QuestNav} class and
 * typically does not need to be used directly by robot code.
 *
 * <h2>Supported Commands</h2>
 *
 * <p>This adapter handles all QuestNav command types including:
 *
 * <ul>
 *   <li><strong>Pose Reset:</strong> Commands to reset the Quest's tracking to a known robot pose
 *   <li><strong>Future Commands:</strong> Extensible for additional command types as needed
 * </ul>
 *
 * <h2>Protocol Buffer Integration</h2>
 *
 * <p>The class implements WPILib's {@link edu.wpi.first.util.protobuf.Protobuf} interface to
 * provide seamless integration with NetworkTables. This enables:
 *
 * <ul>
 *   <li>Efficient binary serialization over the network
 *   <li>Type-safe message handling
 *   <li>Automatic message validation and error handling
 *   <li>Cross-platform compatibility (Java robot code ↔ C# Quest app)
 * </ul>
 *
 * @see gg.questnav.questnav.QuestNav#setPose(edu.wpi.first.math.geometry.Pose3d)
 * @see gg.questnav.questnav.protos.generated.Commands.ProtobufQuestNavCommand
 * @see edu.wpi.first.util.protobuf.Protobuf
 * @since 2025.1.0
 */
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
