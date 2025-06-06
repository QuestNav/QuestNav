package gg.questnav.questnav.protos.wpilib;

import edu.wpi.first.util.protobuf.Protobuf;
import gg.questnav.questnav.protos.generated.Data;
import us.hebi.quickbuf.Descriptors;

public class DeviceDataProto implements Protobuf<Data.ProtobufQuestNavDeviceData, Data.ProtobufQuestNavDeviceData> {
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