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

import gg.questnav.questnav.protos.generated.Commands;
import gg.questnav.questnav.protos.generated.Data;
import gg.questnav.questnav.protos.wpilib.CommandProto;
import gg.questnav.questnav.protos.wpilib.CommandResponseProto;
import gg.questnav.questnav.protos.wpilib.DeviceDataProto;
import java.io.IOException;
import java.io.UncheckedIOException;
import java.net.ServerSocket;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.List;
import java.util.OptionalInt;
import java.util.function.BooleanSupplier;
import java.util.function.Consumer;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.MethodOrderer;
import org.junit.jupiter.api.Order;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.TestMethodOrder;
import org.junit.jupiter.api.io.TempDir;
import org.wpilib.hardware.hal.HAL;
import org.wpilib.math.geometry.Pose3d;
import org.wpilib.math.geometry.Rotation3d;
import org.wpilib.math.geometry.proto.Pose3dProto;
import org.wpilib.networktables.IntegerPublisher;
import org.wpilib.networktables.NetworkTableInstance;
import org.wpilib.networktables.ProtobufPublisher;
import org.wpilib.networktables.ProtobufSubscriber;
import org.wpilib.networktables.PubSubOption;
import org.wpilib.networktables.RawPublisher;
import org.wpilib.networktables.RawSubscriber;
import org.wpilib.system.Timer;

/**
 * Runs QuestNav against a real NT4 connection: the default instance is the robot (server) and a
 * second instance plays the Quest (client), publishing raw frame bytes the way the app does.
 */
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class QuestNavNetworkTest {
  private static final String FRAME_TOPIC = "/QuestNav/frameData";
  private static final String DEVICE_TOPIC = "/QuestNav/deviceData";
  private static final String PROTOCOL_TOPIC = "/QuestNav/protocolVersion";
  private static final String REQUEST_TOPIC = "/QuestNav/request";
  private static final String RESPONSE_TOPIC = "/QuestNav/response";

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
    int port = findFreePort();
    server.startServer(tempDir.resolve("networktables.json").toString(), "127.0.0.1", "", port);

    client = NetworkTableInstance.create();
    client.startClient("questnav-test");
    client.setServer("127.0.0.1", port);
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

  // Must run before anything is published, while the subscribers still hold their defaults
  @Test
  @Order(1)
  void gettersAreEmptyBeforeQuestPublishes() {
    List<Integer> lowBatteryLevels = new ArrayList<>();
    questNav.onLowBattery(20, lowBatteryLevels::add);
    questNav.commandPeriodic();

    assertFalse(questNav.isConnected());
    assertFalse(questNav.isTracking());
    assertTrue(questNav.getBatteryPercent().isEmpty());
    assertTrue(questNav.getFrameCount().isEmpty());
    assertTrue(questNav.getTrackingLostCounter().isEmpty());
    assertTrue(questNav.getAppTimestamp().isEmpty());
    assertTrue(lowBatteryLevels.isEmpty());
  }

  @Test
  void deviceDataIsReportedWhileConnected() throws InterruptedException {
    List<Integer> lowBatteryLevels = new ArrayList<>();
    questNav.onLowBattery(20, lowBatteryLevels::add);
    try (ProtobufPublisher<Data.ProtobufQuestNavDeviceData> devicePublisher =
        client.getProtobufTopic(DEVICE_TOPIC, new DeviceDataProto()).publish()) {
      devicePublisher.set(
          Data.ProtobufQuestNavDeviceData.newInstance()
              .setBatteryPercent(15)
              .setTrackingLostCounter(3));

      // Device data only counts while frames keep the connection alive; retry as in
      // connectionFollowsFrameFlow in case a pause outlasts the 120 ms window
      OptionalInt lostCounter = OptionalInt.empty();
      for (int attempt = 0; attempt < 5; attempt++) {
        waitFor(
            () -> {
              sendFrame(0);
              return questNav.getBatteryPercent().isPresent();
            },
            2.0,
            "device data");
        questNav.commandPeriodic();
        lostCounter = questNav.getTrackingLostCounter();
        if (!lowBatteryLevels.isEmpty() && lostCounter.isPresent()) {
          break;
        }
      }
      assertEquals(List.of(15), lowBatteryLevels);
      assertEquals(OptionalInt.of(3), lostCounter);
      assertTrue(questNav.getAppTimestamp().isPresent());

      Thread.sleep(250);
      questNav.commandPeriodic();
      assertFalse(questNav.isConnected());
      assertTrue(questNav.getBatteryPercent().isEmpty());
      assertTrue(questNav.getTrackingLostCounter().isEmpty());
      assertTrue(questNav.getFrameCount().isPresent());
      assertEquals(List.of(15), lowBatteryLevels);
    }
  }

  @Test
  void connectionFollowsFrameFlow() throws InterruptedException {
    questNav.getAllUnreadPoseFrames();
    // isConnected has a 120 ms window, so retry in case a GC or scheduler pause on a busy runner
    // lands between delivery and the checks
    boolean connected = false;
    double latencyMs = Double.NaN;
    for (int attempt = 0; attempt < 5; attempt++) {
      int count = sendFrame(0);
      waitFor(() -> questNav.getFrameCount().orElse(-1) == count, 2.0, "frame delivery");
      latencyMs = questNav.getLatency();
      connected = questNav.isConnected();
      if (connected && latencyMs > -5 && latencyMs < 100) {
        break;
      }
    }
    assertTrue(connected);
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

  @Test
  void appWithoutProtocolTopicIsLegacy() {
    List<String> warnings = new ArrayList<>();
    Consumer<String> reporter = questNav.warningReporter;
    questNav.warningReporter = warnings::add;
    try {
      // A legacy app sends neither field; the epoch here shows it is ignored for version 1
      waitFor(
          () -> {
            sendFrame(0, 0, 5);
            questNav.commandPeriodic();
            return questNav.getQuestNavProtocolVersion().equals(OptionalInt.of(1))
                && questNav.getResetEpoch().isEmpty();
          },
          2.0,
          "legacy protocol version");
      assertTrue(warnings.isEmpty(), warnings.toString());
    } finally {
      questNav.warningReporter = reporter;
    }
  }

  @Test
  void unsupportedProtocolVersionWarnsOncePerVersion() {
    List<String> warnings = new ArrayList<>();
    Consumer<String> reporter = questNav.warningReporter;
    questNav.warningReporter = warnings::add;
    try (IntegerPublisher protocolPublisher = client.getIntegerTopic(PROTOCOL_TOPIC).publish()) {
      protocolPublisher.set(QuestNav.PROTOCOL_VERSION);
      waitForProtocolVersion(QuestNav.PROTOCOL_VERSION);
      assertTrue(warnings.isEmpty(), warnings.toString());

      protocolPublisher.set(QuestNav.PROTOCOL_VERSION + 1);
      waitForProtocolVersion(QuestNav.PROTOCOL_VERSION + 1);
      assertEquals(1, warnings.size(), warnings.toString());
      assertTrue(warnings.get(0).contains("newer than QuestNavLib"), warnings.get(0));

      protocolPublisher.set(QuestNav.MIN_SUPPORTED_PROTOCOL_VERSION - 1);
      waitForProtocolVersion(QuestNav.MIN_SUPPORTED_PROTOCOL_VERSION - 1);
      assertEquals(2, warnings.size(), warnings.toString());
      assertTrue(warnings.get(1).contains("needs at least version"), warnings.get(1));
    } finally {
      questNav.warningReporter = reporter;
    }
  }

  @Test
  void alignmentStateAndResetEpochAreReported() {
    try (IntegerPublisher protocolPublisher = client.getIntegerTopic(PROTOCOL_TOPIC).publish()) {
      protocolPublisher.set(2);
      questNav.getAllUnreadPoseFrames();
      waitFor(
          () -> {
            sendFrame(0, Data.AlignmentState.ALIGNMENT_STATE_TAG_ALIGNED_VALUE, 7);
            return questNav.getAlignmentState() == AlignmentState.TAG_ALIGNED
                && questNav.getResetEpoch().equals(OptionalInt.of(7));
          },
          2.0,
          "alignment state");

      // Also kept when the frame's capture time falls back to the receive time
      PoseFrame[] frames = questNav.getAllUnreadPoseFrames();
      PoseFrame last = frames[frames.length - 1];
      assertEquals(AlignmentState.TAG_ALIGNED, last.alignmentState());
      assertEquals(7, last.resetEpoch());
    }
  }

  @Test
  void trackingIsLostWhenTheQuestDisconnects() throws InterruptedException {
    List<String> events = new ArrayList<>();
    questNav.onTrackingAcquired(() -> events.add("acquired"));
    questNav.onTrackingLost(() -> events.add("lost"));
    // Wait for each frame so commandPeriodic sees the same state as the check after it
    waitFor(
        () -> {
          int count = sendFrame(0);
          waitFor(() -> questNav.getFrameCount().orElse(-1) == count, 2.0, "frame delivery");
          questNav.commandPeriodic();
          return questNav.isTracking();
        },
        2.0,
        "tracking");

    Thread.sleep(250);
    assertFalse(questNav.isTracking());
    questNav.commandPeriodic();
    assertFalse(events.isEmpty());
    assertEquals("lost", events.get(events.size() - 1));
  }

  @Test
  void commandResponsesAreMatchedById() {
    List<Integer> successes = new ArrayList<>();
    List<Integer> failures = new ArrayList<>();
    questNav.onCommandSuccess(response -> successes.add(response.getCommandId()));
    questNav.onCommandFailure(response -> failures.add(response.getCommandId()));
    try (ProtobufSubscriber<Commands.ProtobufQuestNavCommand> requests =
            client
                .getProtobufTopic(REQUEST_TOPIC, new CommandProto())
                .subscribe(
                    Commands.ProtobufQuestNavCommand.newInstance(),
                    PubSubOption.SEND_ALL,
                    PubSubOption.pollStorage(20));
        ProtobufPublisher<Commands.ProtobufQuestNavCommandResponse> responses =
            client
                .getProtobufTopic(RESPONSE_TOPIC, new CommandResponseProto())
                .publish(PubSubOption.SEND_ALL, PubSubOption.KEEP_DUPLICATES)) {
      questNav.setPose(new Pose3d());
      int first = waitForRequestId(requests);

      // Responses to commands this run hasn't sent, e.g. one left on the topic by an earlier run
      respond(responses, first + 1, true);
      respond(responses, first + 1000, false);
      respond(responses, first, true);
      waitFor(
          () -> {
            questNav.commandPeriodic();
            return !successes.isEmpty();
          },
          2.0,
          "first response");

      // A second response to an answered command is ignored
      respond(responses, first, false);
      questNav.setPose(new Pose3d());
      int second = waitForRequestId(requests);
      assertEquals(first + 1, second);
      respond(responses, second, false);
      waitFor(
          () -> {
            questNav.commandPeriodic();
            return !failures.isEmpty();
          },
          2.0,
          "second response");

      assertEquals(List.of(first), successes);
      assertEquals(List.of(second), failures);
    } finally {
      questNav.onCommandSuccess(null);
      questNav.onCommandFailure(null);
    }
  }

  /** Waits until the app's protocol version reads as {@code version}, then runs a few checks. */
  private static void waitForProtocolVersion(int version) {
    waitFor(
        () -> {
          sendFrame(0);
          return questNav.getQuestNavProtocolVersion().equals(OptionalInt.of(version));
        },
        2.0,
        "protocol version " + version);
    for (int i = 0; i < 10; i++) {
      sendFrame(0);
      questNav.commandPeriodic();
    }
  }

  /** Waits for the next command the robot sends and returns its ID. */
  private static int waitForRequestId(
      ProtobufSubscriber<Commands.ProtobufQuestNavCommand> requests) {
    List<Integer> ids = new ArrayList<>();
    waitFor(
        () -> {
          for (var request : requests.readQueueValues()) {
            ids.add(request.getCommandId());
          }
          return !ids.isEmpty();
        },
        2.0,
        "command request");
    assertEquals(1, ids.size());
    return ids.get(0);
  }

  private static void respond(
      ProtobufPublisher<Commands.ProtobufQuestNavCommandResponse> responses,
      int commandId,
      boolean success) {
    responses.set(
        Commands.ProtobufQuestNavCommandResponse.newInstance()
            .setCommandId(commandId)
            .setSuccess(success)
            .setErrorMessage(success ? "" : "test failure"));
    client.flush();
  }

  /** Publishes one frame from the "Quest" and returns its frame count. */
  private static int sendFrame(long serverTimestampNs) {
    return sendFrame(serverTimestampNs, 0, 0);
  }

  /**
   * Publishes one frame from the "Quest" with an alignment state and reset epoch (0 = not sent) and
   * returns its frame count.
   */
  private static int sendFrame(long serverTimestampNs, int alignmentState, int resetEpoch) {
    var msg =
        Data.ProtobufQuestNavFrameData.newInstance()
            .setFrameCount(++frameCount)
            .setTimestamp(frameCount * 0.02)
            .setIsTracking(true);
    if (alignmentState != 0) {
      msg.setAlignmentStateValue(alignmentState);
    }
    if (resetEpoch != 0) {
      msg.setResetEpoch(resetEpoch);
    }
    new Pose3dProto().pack(msg.getMutablePose3D(), new Pose3d(1, 2, 0, new Rotation3d(0, 0, 0.5)));
    if (serverTimestampNs != 0) {
      msg.setServerTimestamp(serverTimestampNs);
    }
    framePublisher.set(msg.toByteArray());
    client.flush();
    return frameCount;
  }

  private static int findFreePort() {
    try (ServerSocket socket = new ServerSocket(0)) {
      return socket.getLocalPort();
    } catch (IOException e) {
      throw new UncheckedIOException(e);
    }
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
