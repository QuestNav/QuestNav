package frc.robot;

import edu.wpi.first.math.geometry.Pose2d;
import edu.wpi.first.math.geometry.Quaternion;
import edu.wpi.first.math.geometry.Rotation2d;
import edu.wpi.first.math.geometry.Translation2d;
import edu.wpi.first.networktables.*;
import edu.wpi.first.wpilibj.Timer;

import static edu.wpi.first.units.Units.Microseconds;
import static edu.wpi.first.units.Units.Seconds;

/**
 * The QuestNav class provides an interface to communicate with an Oculus/Meta Quest VR headset
 * for robot localization and tracking purposes. It uses NetworkTables to exchange data between
 * the robot and the Quest device.
 */
public class QuestNav {
  // Static inner classes for status and command codes
  public static class Status {
    /** Status indicating system is ready for commands */
    public static final int READY = 0;
    /** Status indicating heading reset completion */
    public static final int HEADING_RESET_COMPLETE = 99;
    /** Status indicating pose reset completion */
    public static final int POSE_RESET_COMPLETE = 98;
    /** Status indicating ping response receipt */
    public static final int PING_RESPONSE = 97;
  }

  public static class Command {
    /** Clear status */
    public static final int CLEAR = 0;
    /** Command to reset the heading */
    public static final int RESET_HEADING = 1;
    /** Command to reset the pose */
    public static final int RESET_POSE = 2;
    /** Command to ping the system */
    public static final int PING = 3;
  }

  /** NetworkTable instance used for communication */
  NetworkTableInstance nt4Instance = NetworkTableInstance.getDefault();

  /** NetworkTable for Quest navigation data */
  NetworkTable nt4Table = nt4Instance.getTable("questnav");

  /** Subscriber for message input from Quest (MISO - Master In Slave Out) */
  private final IntegerSubscriber questMiso = nt4Table.getIntegerTopic("miso").subscribe(-1);

  /** Publisher for message output to Quest (MOSI - Master Out Slave In) */
  private final IntegerPublisher questMosi = nt4Table.getIntegerTopic("mosi").publish();

  /** Subscriber for Quest timestamp data */
  private final DoubleSubscriber questTimestamp = nt4Table.getDoubleTopic("timestamp").subscribe(-1.0f);

  /** Subscriber for raw position data from Quest in Unity coordinate system (in getTranslation, Unity's x becomes FRC's -y, and Unity's z becomes FRC's x) */
  private final FloatArraySubscriber questPosition = nt4Table.getFloatArrayTopic("position").subscribe(new float[]{0.0f, 0.0f, 0.0f});

  /** Subscriber for Quest orientation as quaternion */
  private final FloatArraySubscriber questQuaternion = nt4Table.getFloatArrayTopic("quaternion").subscribe(new float[]{0.0f, 0.0f, 0.0f, 0.0f});

  /** Subscriber for Quest orientation as Euler angles */
  private final FloatArraySubscriber questEulerAngles = nt4Table.getFloatArrayTopic("eulerAngles").subscribe(new float[]{0.0f, 0.0f, 0.0f});

  /** Subscriber for Quest frame counter */
  private final IntegerSubscriber questFrameCount = nt4Table.getIntegerTopic("frameCount").subscribe(-1);

  /** Subscriber for Quest battery percentage */
  private final DoubleSubscriber questBatteryPercent = nt4Table.getDoubleTopic("device/batteryPercent").subscribe(-1.0f);

  /** Subscriber for Quest tracking status */
  private final BooleanSubscriber questIsTracking = nt4Table.getBooleanTopic("device/isTracking").subscribe(false);

  /** Subscriber for Quest tracking loss counter */
  private final IntegerSubscriber questTrackingLostCount = nt4Table.getIntegerTopic("device/trackingLostCounter").subscribe(-1);

  /** Publisher for resetting Quest pose */
  private final DoubleArrayPublisher resetPosePub = nt4Table.getDoubleArrayTopic("resetpose").publish();

  /** Subscriber for heartbeat requests from Quest */
  private final DoubleSubscriber heartbeatRequestSub = nt4Table.getDoubleTopic("heartbeat/quest_to_robot").subscribe(-1.0);

  /** Publisher for heartbeat responses to Quest */
  private final DoublePublisher heartbeatResponsePub = nt4Table.getDoubleTopic("heartbeat/robot_to_quest").publish();

  /** Last processed heartbeat request ID */
  private double lastProcessedHeartbeatId = 0;

  /**
   * Sets the FRC field relative pose of the Quest. This is the QUESTS POSITION, NOT THE ROBOTS!
   * Make sure you correctly offset back from the center of your robot first!
   * */
  public void setPose(Pose2d pose) {
    resetPosePub.set(
            new double[] {
                    pose.getX(),
                    pose.getY(),
                    pose.getRotation().getDegrees()
            });
    questMosi.set(Command.RESET_POSE);
  }

  /**
   * Processes heartbeat requests from the Quest headset and responds with the same ID.
   * This helps maintain connection status between the robot and the Quest.
   * <br/><b>MUST BE RUN IN PERIODIC METHOD</b>
   */
  public void processHeartbeat() {
    double requestId = heartbeatRequestSub.get();
    // Only respond to new requests to avoid flooding
    if (requestId > 0 && requestId != lastProcessedHeartbeatId) {
      heartbeatResponsePub.set(requestId);
      lastProcessedHeartbeatId = requestId;
    }
  }

  /**
   * Gets the battery percentage of the Quest headset.
   *
   * @return The battery percentage as a Double value
   */
  public Double getBatteryPercent() {
    return questBatteryPercent.get();
  }

  /**
   * Gets the current tracking state of the Quest headset.
   *
   * @return Boolean indicating if the Quest is currently tracking (true) or not (false)
   */
  public Boolean getTrackingStatus() {
    return questIsTracking.get();
  }

  /**
   * Gets the current frame count from the Quest headset.
   *
   * @return The frame count as a Long value
   */
  public Long getFrameCount() {
    return questFrameCount.get();
  }

  /**
   * Gets the number of tracking lost events since the Quest connected to the robot.
   *
   * @return The tracking lost counter as a Long value
   */
  public Long getTrackingLostCounter() {
    return questTrackingLostCount.get();
  }

  /**
   * Determines if the Quest headset is currently connected to the robot.
   * Connection is determined by checking when the last battery update was received.
   *
   * @return Boolean indicating if the Quest is connected (true) or not (false)
   */
  public Boolean getConnected() {
    return Seconds.of(Timer.getTimestamp())
            .minus(Microseconds.of(questTimestamp.getLastChange()))
            .lt(Seconds.of(0.25));
  }

  /**
   * Gets the orientation of the Quest headset as a Quaternion.
   *
   * @return The orientation as a Quaternion object
   */
  public Quaternion getQuaternion() {
    float[] qqFloats = questQuaternion.get();
    return new Quaternion(qqFloats[0], qqFloats[1], qqFloats[2], qqFloats[3]);
  }

  /**
   * Gets the Quest's timestamp in NetworkTables server time.
   *
   * @return The timestamp as a double value
   */
  public double getTimestamp() {
    return questTimestamp.getAtomic().serverTime;
  }

  /**
   * Cleans up Quest navigation subroutine messages after processing on the headset.
   * Resets the MOSI value to zero if MISO is non-zero.
   * <br/><b>MUST BE RUN IN PERIODIC METHOD</b>
   */
  public void cleanupResponses() {
    if (questMiso.get() != Status.READY) {
      switch ((int) questMiso.get()) {
        case Status.POSE_RESET_COMPLETE -> {
          questMosi.set(Command.CLEAR);
        }
        case Status.HEADING_RESET_COMPLETE -> {
          questMosi.set(Command.CLEAR);
        }
        case Status.PING_RESPONSE -> {
          questMosi.set(Command.CLEAR);
        }
      }
    }
  }

  /**
   * Converts the raw QuestNav yaw to a Rotation2d object. Applies necessary coordinate system
   * transformations.
   *
   * @return Rotation2d representing the headset's yaw
   */
  private Rotation2d getYaw() {
    return Rotation2d.fromDegrees(-questEulerAngles.get()[1]);
  }

  /**
   * Gets the position of the Quest headset as a Translation2d object.
   * Converts the Quest coordinate system to the robot coordinate system.
   *
   * @return The position as a Translation2d object
   */
  private Translation2d getTranslation() {
    float[] questnavPosition = questPosition.get();
    return new Translation2d(questnavPosition[2], -questnavPosition[0]);
  }

  /**
   * Gets the complete pose (position and orientation) of the Quest headset.
   *
   * @return The pose as a Pose2d object
   */
  public Pose2d getPose() {
    return new Pose2d(getTranslation(), getYaw());
  }
}