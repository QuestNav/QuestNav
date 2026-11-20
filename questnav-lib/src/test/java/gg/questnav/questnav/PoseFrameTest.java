/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2026 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.lang.reflect.RecordComponent;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.Arrays;
import org.junit.jupiter.api.Test;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.Rotation3d;
import org.wpilib.util.struct.Struct;
import org.wpilib.util.struct.StructGenerator;

class PoseFrameTest {
  @Test
  void componentOrderIsStable() {
    // Robot code constructs and destructures PoseFrame positionally; two adjacent doubles make a
    // reorder silently swap the timestamps. New components go at the end.
    RecordComponent[] components = PoseFrame.class.getRecordComponents();
    assertArrayEquals(
        new String[] {
          "questPose3d",
          "dataTimestamp",
          "appTimestamp",
          "frameCount",
          "isTracking",
          "alignmentState",
          "resetEpoch"
        },
        Arrays.stream(components).map(RecordComponent::getName).toArray());
    assertArrayEquals(
        new Class<?>[] {
          Pose3d.class,
          double.class,
          double.class,
          int.class,
          boolean.class,
          AlignmentState.class,
          int.class
        },
        Arrays.stream(components).map(RecordComponent::getType).toArray());
  }

  @Test
  void constructorWithoutAlignmentStillWorks() throws ReflectiveOperationException {
    // Code compiled against the five-component record calls this constructor
    PoseFrame.class.getConstructor(
        Pose3d.class, double.class, double.class, int.class, boolean.class);

    var frame = new PoseFrame(new Pose3d(), 1.0, 2.0, 3, true);
    assertEquals(AlignmentState.UNKNOWN, frame.alignmentState());
    assertEquals(0, frame.resetEpoch());
  }

  @Test
  void nullAlignmentStateIsUnknown() {
    var frame = new PoseFrame(new Pose3d(), 1.0, 2.0, 3, true, null, 4);
    assertEquals(AlignmentState.UNKNOWN, frame.alignmentState());
  }

  @Test
  void recordStructCanBeGenerated() {
    // Logging frameworks log records with genRecord, which gives an empty struct (size 0) when a
    // component can't be serialized
    Struct<PoseFrame> struct = StructGenerator.genRecord(PoseFrame.class);
    assertTrue(struct.getSize() > 0);
    assertTrue(struct.getSchema().contains("alignmentState"), struct.getSchema());
    assertTrue(struct.getSchema().contains("resetEpoch"), struct.getSchema());

    for (AlignmentState state : AlignmentState.values()) {
      var frame =
          new PoseFrame(
              new Pose3d(1, 2, 3, new Rotation3d(0.1, 0.2, 0.3)), 1.5, 2.5, 42, true, state, -7);
      ByteBuffer buffer = ByteBuffer.allocate(struct.getSize()).order(ByteOrder.LITTLE_ENDIAN);
      struct.pack(buffer, frame);
      buffer.rewind();
      assertEquals(frame, struct.unpack(buffer));
    }
  }
}
