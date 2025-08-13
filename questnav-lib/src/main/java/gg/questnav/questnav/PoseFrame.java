/*
* QUESTNAV
  https://github.com/QuestNav
* Copyright (C) 2025 QuestNav
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the MIT License as published.
*/
package gg.questnav.questnav;

import edu.wpi.first.math.geometry.Pose3d;

/**
 * A frame of data from the Quest.
 *
 * @param questPose3d The current pose of the Quest on the field. This will only return the
 *     field-relative pose if {@link QuestNav#setPose(Pose3d)} has been called at least once.
 * @param dataTimestamp The NT timestamp of when the last frame data was sent. This is the value
 *     which should be used with a pose estimator.
 * @param appTimestamp The Quest app's uptime timestamp. For integration with a pose estimator, use
 *     the timestamp from {@link #dataTimestamp()} instead!
 * @param frameCount The current frame count.
 */
public record PoseFrame(
    Pose3d questPose3d, double dataTimestamp, double appTimestamp, int frameCount) {}
