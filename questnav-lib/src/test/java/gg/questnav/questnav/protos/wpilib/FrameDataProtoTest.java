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

import gg.questnav.questnav.AlignmentState;
import gg.questnav.questnav.PoseFrame;
import gg.questnav.questnav.protos.generated.Data;
import java.io.IOException;
import java.io.InputStream;
import org.junit.jupiter.api.Test;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.Quaternion;
import org.wpilib.math.geometry.Rotation3d;
import org.wpilib.math.geometry.Translation3d;
import org.wpilib.util.protobuf.ProtobufBuffer;

class FrameDataProtoTest {
  private final FrameDataProto proto = new FrameDataProto();

  @Test
  void roundTripsAllFields() throws Exception {
    var pose = new Pose3d(1.25, -2.5, 0.75, new Rotation3d(0.1, -0.2, 2.9));
    var frame =
        new PoseFrame(pose, 123.456789012, 42.125, 9876, true, AlignmentState.REALIGNING, 77);

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
    assertEquals(AlignmentState.REALIGNING, decoded.alignmentState());
    assertEquals(77, decoded.resetEpoch());
  }

  @Test
  void roundTripsEveryAlignmentState() throws Exception {
    for (AlignmentState state : AlignmentState.values()) {
      var msg = proto.createMessage();
      proto.pack(msg, new PoseFrame(new Pose3d(), 1.0, 2.0, 3, true, state, 4));
      var decoded = proto.unpack(Data.ProtobufQuestNavFrameData.parseFrom(msg.toByteArray()));
      assertEquals(state, decoded.alignmentState());
    }
  }

  @Test
  void unknownAlignmentStateUnpacksAsUnknown() {
    // A state from an app newer than this library
    var msg = Data.ProtobufQuestNavFrameData.newInstance().setAlignmentStateValue(99);
    assertEquals(AlignmentState.UNKNOWN, proto.unpack(msg).alignmentState());
  }

  /** Same bytes as FrameDataGoldenTests.AlignmentFieldsEncodeToGoldenBytes on the C# side. */
  @Test
  void decodesAlignmentFieldsFromTheApp() throws IOException {
    byte[] golden = {0x30, 0x03, 0x38, (byte) 0xAC, 0x02};

    var frame = proto.unpack(Data.ProtobufQuestNavFrameData.parseFrom(golden));
    assertEquals(AlignmentState.TAG_ALIGNED, frame.alignmentState());
    assertEquals(300, frame.resetEpoch());
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
  void decodingDoesNotKeepFieldsOfThePreviousFrame() throws IOException {
    // NT subscribers decode every frame into one reused message
    var buffer = ProtobufBuffer.create(proto);
    var pose = new Pose3d(1.25, -2.5, 0.75, new Rotation3d(0.1, -0.2, 2.9));
    var msg = proto.createMessage();
    proto.pack(msg, new PoseFrame(pose, 5.0, 2.0, 3, true));
    var first = buffer.read(msg.toByteArray());
    assertEquals(pose, first.questPose3d());
    assertEquals(5.0, first.dataTimestamp());

    // The app's encoder leaves out zero values, as here for a lost-tracking frame at the origin
    // sent before time sync, which has no server timestamp
    var lostMsg = Data.ProtobufQuestNavFrameData.newInstance().setFrameCount(4).setTimestamp(2.5);
    lostMsg.getMutablePose3D().getMutableTranslation();
    lostMsg.getMutablePose3D().getMutableRotation().getMutableQ().setW(1.0);
    var lost = buffer.read(lostMsg.toByteArray());
    assertEquals(new Pose3d(), lost.questPose3d());
    // 0 tells QuestNav to fall back to the NT time; the previous frame's time would be wrong
    assertEquals(0.0, lost.dataTimestamp());
    assertEquals(2.5, lost.appTimestamp());
    assertEquals(4, lost.frameCount());
    assertFalse(lost.isTracking());
  }

  @Test
  void missingServerTimestampUnpacksAsZero() {
    var msg = Data.ProtobufQuestNavFrameData.newInstance().setTimestamp(3.5).setFrameCount(7);
    assertEquals(0.0, proto.unpack(msg).dataTimestamp());
  }

  /**
   * frame_data_golden.bin was encoded by the Unity app's C# protobuf code; FrameDataGoldenTests on
   * the C# side checks it still encodes the same bytes.
   */
  @Test
  void decodesGoldenBytesFromTheApp() throws IOException {
    byte[] golden;
    try (InputStream in = getClass().getResourceAsStream("frame_data_golden.bin")) {
      golden = in.readAllBytes();
    }

    var msg = Data.ProtobufQuestNavFrameData.parseFrom(golden);
    var frame = proto.unpack(msg);
    assertEquals(
        new Pose3d(
            new Translation3d(1.5, -2.25, 0.125),
            new Rotation3d(new Quaternion(0.5, 0.5, 0.5, 0.5))),
        frame.questPose3d());
    assertEquals(1_234_567_890_123L, msg.getServerTimestamp());
    assertEquals(1_234.567890123, frame.dataTimestamp(), 1e-9);
    assertEquals(123.456, frame.appTimestamp());
    assertEquals(4242, frame.frameCount());
    assertTrue(frame.isTracking());
    // Encoded before protocol version 2
    assertEquals(AlignmentState.UNKNOWN, frame.alignmentState());
    assertEquals(0, frame.resetEpoch());
  }

  @Test
  void isImmutable() {
    assertTrue(PoseFrame.proto.isImmutable());
  }
}
