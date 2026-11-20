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

import gg.questnav.questnav.protos.generated.Commands;
import gg.questnav.questnav.protos.generated.Data;
import java.io.IOException;
import org.junit.jupiter.api.Test;
import org.wpilib.util.protobuf.ProtobufBuffer;

/**
 * NT subscribers decode every value into one reused message; a value must not keep the fields of
 * the one before it that it leaves out (the app's encoder leaves out zero values).
 */
class MessageProtoTest {
  @Test
  void failureResponseAfterSuccessIsAFailure() throws IOException {
    var buffer = ProtobufBuffer.create(new CommandResponseProto());
    var success =
        buffer.read(
            Commands.ProtobufQuestNavCommandResponse.newInstance()
                .setCommandId(1)
                .setSuccess(true)
                .toByteArray());
    assertTrue(success.getSuccess());

    var failure =
        buffer.read(
            Commands.ProtobufQuestNavCommandResponse.newInstance()
                .setCommandId(2)
                .setErrorMessage("Unknown command type")
                .toByteArray());
    assertEquals(2, failure.getCommandId());
    assertFalse(failure.getSuccess());
    // Values already returned keep their contents
    assertTrue(success.getSuccess());
  }

  @Test
  void deviceDataDoesNotKeepPreviousValues() throws IOException {
    var buffer = ProtobufBuffer.create(new DeviceDataProto());
    buffer.read(
        Data.ProtobufQuestNavDeviceData.newInstance()
            .setBatteryPercent(50)
            .setTrackingLostCounter(2)
            .toByteArray());
    var data =
        buffer.read(
            Data.ProtobufQuestNavDeviceData.newInstance().setBatteryPercent(40).toByteArray());
    assertEquals(40, data.getBatteryPercent());
    assertEquals(0, data.getTrackingLostCounter());
  }

  @Test
  void commandDoesNotKeepPreviousValues() throws IOException {
    var buffer = ProtobufBuffer.create(new CommandProto());
    var reset = Commands.ProtobufQuestNavCommand.newInstance().setCommandId(1);
    reset.getMutablePoseResetPayload().getMutableTargetPose().getMutableTranslation().setX(3.0);
    buffer.read(reset.toByteArray());

    var command =
        buffer.read(Commands.ProtobufQuestNavCommand.newInstance().setCommandId(2).toByteArray());
    assertEquals(2, command.getCommandId());
    assertFalse(command.hasPoseResetPayload());
  }
}
