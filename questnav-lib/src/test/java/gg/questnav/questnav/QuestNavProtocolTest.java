/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2026 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class QuestNavProtocolTest {
  @Test
  void supportedProtocolVersionsDoNotWarn() {
    for (int version = QuestNav.MIN_SUPPORTED_PROTOCOL_VERSION;
        version <= QuestNav.PROTOCOL_VERSION;
        version++) {
      assertNull(QuestNav.protocolVersionWarning(version, "app", "lib"));
    }
  }

  @Test
  void differentReleaseVersionsDoNotWarn() {
    assertNull(
        QuestNav.protocolVersionWarning(QuestNav.PROTOCOL_VERSION, "2026-2.1.0", "2027-1.0.0"));
  }

  @Test
  void olderAppWarnsToUpdateTheApp() {
    String warning =
        QuestNav.protocolVersionWarning(
            QuestNav.MIN_SUPPORTED_PROTOCOL_VERSION - 1, "2025-1.0.0", "2027-1.0.0");
    assertNotNull(warning);
    assertTrue(warning.contains("2025-1.0.0"), warning);
    assertTrue(warning.contains("Update the QuestNav app"), warning);
  }

  @Test
  void newerAppWarnsToUpdateTheLibrary() {
    String warning =
        QuestNav.protocolVersionWarning(QuestNav.PROTOCOL_VERSION + 1, "2028-1.0.0", "2027-1.0.0");
    assertNotNull(warning);
    assertTrue(warning.contains("protocol version " + (QuestNav.PROTOCOL_VERSION + 1)), warning);
    assertTrue(warning.contains("update the QuestNavLib vendordep"), warning);
  }
}
