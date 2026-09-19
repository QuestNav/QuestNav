/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav.protos.wpilib;

import static org.wpilib.units.Units.Nanoseconds;
import static org.wpilib.units.Units.Seconds;

import gg.questnav.questnav.PoseFrame;
import gg.questnav.questnav.protos.generated.Data;
import gg.questnav.questnav.protos.generated.Data.ProtobufQuestNavFrameData;
import org.wpilib.math.geometry.proto.Pose3dProto;
import org.wpilib.math.geometry.proto.detail.ProtobufPose3d;
import org.wpilib.util.protobuf.Protobuf;
import us.hebi.quickbuf.Descriptors;

/** WPILib Protobuf layer for FrameData Protobuf */
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
  public PoseFrame unpack(Data.ProtobufQuestNavFrameData msg) {
    return new PoseFrame(
        pose3dProto.unpack(msg.getPose3D()),
        msg.getTimestamp(),
        Nanoseconds.of(msg.getServerTimestamp()).in(Seconds),
        msg.getFrameCount(),
        msg.getIsTracking());
  }

  @Override
  public void pack(ProtobufQuestNavFrameData msg, PoseFrame value) {
    var newPros3dProto = ProtobufPose3d.newInstance();
    pose3dProto.pack(newPros3dProto, value.questPose3d());

    msg.setPose3D(newPros3dProto);
    msg.setTimestamp(value.appTimestamp());
    msg.setServerTimestamp((long) Seconds.of(value.dataTimestamp()).in(Nanoseconds));
    msg.setFrameCount(value.frameCount());
    msg.setIsTracking(value.isTracking());
  }
}
