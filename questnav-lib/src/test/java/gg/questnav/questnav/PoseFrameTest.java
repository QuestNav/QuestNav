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

import java.lang.reflect.RecordComponent;
import java.util.Arrays;
import org.junit.jupiter.api.Test;
import org.wpilib.math.geometry.Pose3d;

class PoseFrameTest {
  @Test
  void componentOrderIsStable() {
    // Robot code constructs and destructures PoseFrame positionally; two adjacent doubles make a
    // reorder silently swap the timestamps.
    RecordComponent[] components = PoseFrame.class.getRecordComponents();
    assertArrayEquals(
        new String[] {"questPose3d", "dataTimestamp", "appTimestamp", "frameCount", "isTracking"},
        Arrays.stream(components).map(RecordComponent::getName).toArray());
    assertArrayEquals(
        new Class<?>[] {Pose3d.class, double.class, double.class, int.class, boolean.class},
        Arrays.stream(components).map(RecordComponent::getType).toArray());
  }
}
