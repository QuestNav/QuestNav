/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2026 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav.protos.wpilib;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import gg.questnav.questnav.PoseFrame;
import gg.questnav.questnav.protos.generated.Data;
import org.junit.jupiter.api.Test;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.Rotation3d;

class FrameDataProtoTest {
  private final FrameDataProto proto = new FrameDataProto();

  @Test
  void roundTripsAllFields() throws Exception {
    var pose = new Pose3d(1.25, -2.5, 0.75, new Rotation3d(0.1, -0.2, 2.9));
    var frame = new PoseFrame(pose, 123.456789012, 42.125, 9876, true);

    var msg = proto.createMessage();
    proto.pack(msg, frame);
    // dataTimestamp is carried as integer nanoseconds, appTimestamp as seconds
    assertEquals(123_456_789_012L, msg.getServerTimestamp());
    assertEquals(42.125, msg.getTimestamp());

    var decoded = proto.unpack(Data.ProtobufQuestNavFrameData.parseFrom(msg.toByteArray()));
    assertEquals(pose, decoded.questPose3d());
    assertEquals(123.456789012, decoded.dataTimestamp(), 1e-9);
    assertEquals(42.125, decoded.appTimestamp());
    assertEquals(9876, decoded.frameCount());
    assertTrue(decoded.isTracking());
  }

  @Test
  void packReusesMessage() {
    var msg = proto.createMessage();
    proto.pack(msg, new PoseFrame(new Pose3d(1, 2, 3, new Rotation3d()), 1.0, 2.0, 3, true));
    proto.pack(msg, new PoseFrame(new Pose3d(), 4.0, 5.0, 6, false));

    var decoded = proto.unpack(msg);
    assertEquals(new Pose3d(), decoded.questPose3d());
    assertEquals(4.0, decoded.dataTimestamp());
    assertEquals(5.0, decoded.appTimestamp());
    assertEquals(6, decoded.frameCount());
    assertFalse(decoded.isTracking());
  }

  @Test
  void missingServerTimestampUnpacksAsZero() {
    var msg = Data.ProtobufQuestNavFrameData.newInstance().setTimestamp(3.5).setFrameCount(7);
    assertEquals(0.0, proto.unpack(msg).dataTimestamp());
  }

  @Test
  void isImmutable() {
    assertTrue(PoseFrame.proto.isImmutable());
  }
}
