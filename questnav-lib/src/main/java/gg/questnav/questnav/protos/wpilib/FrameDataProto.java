/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav.protos.wpilib;

import gg.questnav.questnav.PoseFrame;
import gg.questnav.questnav.protos.generated.Data;
import org.wpilib.math.geometry.proto.Pose3dProto;
import org.wpilib.util.protobuf.Protobuf;
import us.hebi.quickbuf.Descriptors;

/**
 * WPILib Protobuf layer for FrameData Protobuf. Serializes {@link PoseFrame} (it serialized the raw
 * {@code ProtobufQuestNavFrameData} message before 2027).
 *
 * <p>{@code dataTimestamp} is carried as nanoseconds in the message's {@code server_timestamp}
 * field; 0 means the sender did not provide one.
 */
public class FrameDataProto implements Protobuf<PoseFrame, Data.ProtobufQuestNavFrameData> {

  /** Protobuf instance for Pose3d */
  private final Pose3dProto pose3dProto = new Pose3dProto();

  @Override
  public Class<PoseFrame> getTypeClass() {
    return PoseFrame.class;
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
  public PoseFrame unpack(Data.ProtobufQuestNavFrameData received) {
    // WPILib decodes into a reused message after clearQuick(), which keeps the values of fields
    // a message leaves out (such as false or 0); copying only the fields it has drops them
    var msg = Data.ProtobufQuestNavFrameData.newInstance().mergeFrom(received);
    return new PoseFrame(
        pose3dProto.unpack(msg.getPose3D()),
        msg.getServerTimestamp() * 1e-9,
        msg.getTimestamp(),
        msg.getFrameCount(),
        msg.getIsTracking());
  }

  @Override
  public void pack(Data.ProtobufQuestNavFrameData msg, PoseFrame value) {
    pose3dProto.pack(msg.getMutablePose3D(), value.questPose3d());
    msg.setServerTimestamp(Math.round(value.dataTimestamp() * 1e9));
    msg.setTimestamp(value.appTimestamp());
    msg.setFrameCount(value.frameCount());
    msg.setIsTracking(value.isTracking());
  }

  @Override
  public boolean isImmutable() {
    return true;
  }
}
