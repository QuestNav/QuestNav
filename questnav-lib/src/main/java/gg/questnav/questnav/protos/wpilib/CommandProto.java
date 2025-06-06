package gg.questnav.questnav.protos.wpilib;

import edu.wpi.first.util.protobuf.Protobuf;
import gg.questnav.questnav.protos.generated.Commands;
import us.hebi.quickbuf.Descriptors;

public class CommandProto implements Protobuf<Commands.ProtobufQuestNavCommand, Commands.ProtobufQuestNavCommand> {
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