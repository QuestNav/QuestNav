/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2026 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import org.wpilib.util.struct.Struct;
import org.wpilib.util.struct.StructGenerator;
import org.wpilib.util.struct.StructSerializable;

/**
 * What the pose the Quest reports is referenced to.
 *
 * <p>Struct logs store the constant's ordinal, so new constants go at the end.
 *
 * @see PoseFrame#alignmentState()
 * @see QuestNav#getAlignmentState()
 */
public enum AlignmentState implements StructSerializable {
  /**
   * Not known: the QuestNav app is too old to report it (protocol version 1), it reported a state
   * this library doesn't know, or there is no data.
   */
  UNKNOWN,

  /** Raw tracking frame: nothing has placed the pose on the field yet. */
  UNALIGNED,

  /**
   * Placed on the field by a pose reset ({@link QuestNav#setPose} or the web UI), and not yet
   * aligned by AprilTags. The next accepted AprilTag alignment replaces it.
   */
  POSE_RESET,

  /** Aligned to the field by AprilTags; later tag observations correct the position. */
  TAG_ALIGNED,

  /**
   * Was placed on the field (by AprilTags or a pose reset), but the placement is no longer trusted:
   * the tags have consistently disagreed with the pose, or tracking relocalized after an outage.
   * The pose continues from the old placement until the tags re-align it.
   */
  REALIGNING;

  /** Struct serializer, so {@link PoseFrame} can be logged as a struct */
  public static final Struct<AlignmentState> struct = StructGenerator.genEnum(AlignmentState.class);
}
