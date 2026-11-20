/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2026 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.fail;

import gg.questnav.questnav.protos.generated.Data;
import java.io.IOException;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.function.BooleanSupplier;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import org.wpilib.hardware.hal.HAL;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.Rotation3d;
import org.wpilib.math.geometry.proto.Pose3dProto;
import org.wpilib.networktables.NetworkTableInstance;
import org.wpilib.networktables.PubSubOption;
import org.wpilib.networktables.RawPublisher;
import org.wpilib.networktables.RawSubscriber;
import org.wpilib.system.Timer;

/**
 * Runs QuestNav against a real NT4 connection: the default instance is the robot (server) and a
 * second instance plays the Quest (client), publishing raw frame bytes the way the app does.
 */
class QuestNavNetworkTest {
  private static final int PORT = 5817;
  private static final String FRAME_TOPIC = "/QuestNav/frameData";

  @TempDir static Path tempDir;

  private static NetworkTableInstance server;
  private static NetworkTableInstance client;
  private static RawPublisher framePublisher;
  private static QuestNav questNav;
  private static int frameCount = 0;

  @BeforeAll
  static void setUp() {
    assertTrue(HAL.initialize());
    server = NetworkTableInstance.getDefault();
    questNav = new QuestNav();
    server.startServer(tempDir.resolve("networktables.json").toString(), "127.0.0.1", "", PORT);

    client = NetworkTableInstance.create();
    client.startClient("questnav-test");
    client.setServer("127.0.0.1", PORT);
    waitFor(() -> client.getServerTimeOffset().isPresent(), 5.0, "client time sync");

    framePublisher =
        client
            .getRawTopic(FRAME_TOPIC)
            .publish(PoseFrame.proto.getTypeString(), PubSubOption.SEND_ALL);
  }

  @AfterAll
  static void tearDown() {
    framePublisher.close();
    client.close();
    server.stopServer();
  }

  @Test
  void connectionFollowsFrameFlow() throws InterruptedException {
    questNav.getAllUnreadPoseFrames();
    int count = sendFrame(0);
    waitFor(() -> questNav.getFrameCount().orElse(-1) == count, 2.0, "frame delivery");

    assertTrue(questNav.isConnected());
    double latencyMs = questNav.getLatency();
    assertTrue(latencyMs > -5 && latencyMs < 100, "latency " + latencyMs + " ms");

    Thread.sleep(250);
    assertFalse(questNav.isConnected());
  }

  @Test
  void missingCaptureTimeFallsBackToServerTime() throws IOException {
    // A second subscriber sees the same values and their NT server timestamps
    try (RawSubscriber raw =
        server
            .getRawTopic(FRAME_TOPIC)
            .subscribe(
                PoseFrame.proto.getTypeString(),
                new byte[0],
                PubSubOption.SEND_ALL,
                PubSubOption.pollStorage(20))) {
      questNav.getAllUnreadPoseFrames();
      // 0 = what apps that predate the field send (proto3 omits it on the wire)
      int legacyCount = sendFrame(0);
      long embeddedNs = Math.round((Timer.getTimestamp() - 0.5) * 1e9);
      int embeddedCount = sendFrame(embeddedNs);

      List<PoseFrame> frames = new ArrayList<>();
      waitFor(
          () -> {
            frames.addAll(List.of(questNav.getAllUnreadPoseFrames()));
            return frames.size() >= 2;
          },
          2.0,
          "frame delivery");
      assertEquals(2, frames.size());
      assertEquals(legacyCount, frames.get(0).frameCount());
      assertEquals(embeddedCount, frames.get(1).frameCount());

      long legacyServerTime = 0;
      for (var value : raw.readQueue()) {
        if (Data.ProtobufQuestNavFrameData.parseFrom(value.value).getFrameCount() == legacyCount) {
          legacyServerTime = value.serverTime;
        }
      }
      assertNotEquals(0, legacyServerTime);
      assertEquals(legacyServerTime * 1e-9, frames.get(0).dataTimestamp());
      assertEquals(embeddedNs * 1e-9, frames.get(1).dataTimestamp());
    }
  }

  /** Publishes one frame from the "Quest" and returns its frame count. */
  private static int sendFrame(long serverTimestampNs) {
    var msg =
        Data.ProtobufQuestNavFrameData.newInstance()
            .setFrameCount(++frameCount)
            .setTimestamp(frameCount * 0.02)
            .setIsTracking(true);
    new Pose3dProto().pack(msg.getMutablePose3D(), new Pose3d(1, 2, 0, new Rotation3d(0, 0, 0.5)));
    if (serverTimestampNs != 0) {
      msg.setServerTimestamp(serverTimestampNs);
    }
    framePublisher.set(msg.toByteArray());
    client.flush();
    return frameCount;
  }

  private static void waitFor(BooleanSupplier condition, double timeoutSeconds, String what) {
    long deadline = System.nanoTime() + (long) (timeoutSeconds * 1e9);
    while (!condition.getAsBoolean()) {
      if (System.nanoTime() > deadline) {
        fail("Timed out waiting for " + what);
      }
      try {
        Thread.sleep(1);
      } catch (InterruptedException e) {
        Thread.currentThread().interrupt();
        fail("Interrupted waiting for " + what);
      }
    }
  }
}
