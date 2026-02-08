using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a 3D pose containing translational and rotational elements.
    /// </summary>
    /// <remarks>
    /// A Pose3d represents the position and orientation of an object in 3D space.
    /// It combines a Translation3d (position) and a Rotation3d (orientation).
    /// </remarks>
    public class Pose3d : IEquatable<Pose3d>
    {
        /// <summary>
        /// A preallocated Pose3d representing the origin with no rotation.
        /// </summary>
        /// <remarks>
        /// This exists to avoid allocations for common poses.
        /// </remarks>
        public static readonly Pose3d Zero = new Pose3d();

        /// <summary>
        /// Gets the translational component of the pose.
        /// </summary>
        [JsonProperty("translation")]
        public Translation3d Translation { get; }

        /// <summary>
        /// Gets the rotational component of the pose.
        /// </summary>
        [JsonProperty("rotation")]
        public Rotation3d Rotation { get; }

        /// <summary>
        /// Gets the X component of the pose's translation.
        /// </summary>
        public double X => Translation.X;

        /// <summary>
        /// Gets the Y component of the pose's translation.
        /// </summary>
        public double Y => Translation.Y;

        /// <summary>
        /// Gets the Z component of the pose's translation.
        /// </summary>
        public double Z => Translation.Z;

        /// <summary>
        /// Constructs a pose at the origin facing toward the positive X axis.
        /// </summary>
        public Pose3d()
        {
            Translation = Translation3d.Zero;
            Rotation = Rotation3d.Zero;
        }

        /// <summary>
        /// Constructs a pose with the specified translation and rotation.
        /// </summary>
        /// <param name="translation">The translational component of the pose.</param>
        /// <param name="rotation">The rotational component of the pose.</param>
        [JsonConstructor]
        public Pose3d(Translation3d translation, Rotation3d rotation)
        {
            this.Translation = translation ?? throw new ArgumentNullException(nameof(translation));
            this.Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        }

        /// <summary>
        /// Constructs a pose with x, y, and z translations instead of a separate Translation3d.
        /// </summary>
        /// <param name="x">The x component of the translational component of the pose.</param>
        /// <param name="y">The y component of the translational component of the pose.</param>
        /// <param name="z">The z component of the translational component of the pose.</param>
        /// <param name="rotation">The rotational component of the pose.</param>
        public Pose3d(double x, double y, double z, Rotation3d rotation)
        {
            Translation = new Translation3d(x, y, z);
            this.Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        }

        /// <summary>
        /// Constructs a pose with the specified 4x4 affine transformation matrix.
        /// </summary>
        /// <param name="matrix">The 4x4 affine transformation matrix.</param>
        /// <exception cref="ArgumentException">Thrown if the affine transformation matrix is invalid.</exception>
        /// <remarks>
        /// The matrix must be in the standard affine transformation format:
        /// [R | t]
        /// [0 | 1]
        /// where R is a 3x3 rotation matrix and t is a 3x1 translation vector.
        /// The bottom row must be [0, 0, 0, 1].
        /// </remarks>
        public Pose3d(double[,] matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.GetLength(0) != 4 || matrix.GetLength(1) != 4)
                throw new ArgumentException("Matrix must be 4x4", nameof(matrix));

            // Validate the bottom row
            if (
                matrix[3, 0] != 0.0
                || matrix[3, 1] != 0.0
                || matrix[3, 2] != 0.0
                || matrix[3, 3] != 1.0
            )
            {
                throw new ArgumentException(
                    "Affine transformation matrix is invalid - bottom row must be [0, 0, 0, 1]"
                );
            }

            // Extract translation from the last column
            Translation = new Translation3d(matrix[0, 3], matrix[1, 3], matrix[2, 3]);

            // Extract the 3x3 rotation matrix (top-left block)
            double[,] rotationMatrix = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rotationMatrix[i, j] = matrix[i, j];
                }
            }

            Rotation = new Rotation3d(rotationMatrix);
        }

        /// <summary>
        /// Creates a new Pose3d from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf to convert</param>
        public Pose3d(ProtobufPose3d protobuf)
        {
            Translation = new Translation3d(protobuf.Translation);
            Rotation = new Rotation3d(protobuf.Rotation);
        }

        /// <summary>
        /// Copies the data to a protobuf-sterilized version to be sent over NT
        /// </summary>
        /// <returns>The sterilized protobuf</returns>
        public ProtobufPose3d ToProtobuf()
        {
            var proto = new ProtobufPose3d
            {
                Translation = Translation.ToProtobuf(),
                Rotation = Rotation.ToProtobuf(),
            };
            return proto;
        }

        /// <summary>
        /// Transforms the pose by the given transformation and returns the new transformed pose.
        /// </summary>
        /// <param name="other">The transform to transform the pose by.</param>
        /// <returns>The transformed pose.</returns>
        /// <remarks>
        /// The transform is applied relative to the pose's frame. Note that this differs from
        /// <see cref="RotateBy"/>, which is applied relative to the global frame and around the origin.
        /// </remarks>
        public Pose3d Plus(Transform3d other)
        {
            return TransformBy(other);
        }

        /// <summary>
        /// Returns the Transform3d that maps the other pose to this pose.
        /// </summary>
        /// <param name="other">The initial pose of the transformation.</param>
        /// <returns>The transform that maps the other pose to the current pose.</returns>
        public Transform3d Minus(Pose3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var pose = RelativeTo(other);
            return new Transform3d(pose.Translation, pose.Rotation);
        }

        /// <summary>
        /// Multiplies the current pose by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar multiplier.</param>
        /// <returns>The new scaled Pose3d.</returns>
        /// <remarks>
        /// Both the translation and rotation components are scaled.
        /// </remarks>
        public Pose3d Times(double scalar)
        {
            return new Pose3d(Translation.Times(scalar), Rotation.Times(scalar));
        }

        /// <summary>
        /// Divides the current pose by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar divisor.</param>
        /// <returns>The new scaled Pose3d.</returns>
        public Pose3d Div(double scalar)
        {
            return Times(1.0 / scalar);
        }

        /// <summary>
        /// Rotates the pose around the origin and returns the new pose.
        /// </summary>
        /// <param name="other">The rotation to transform the pose by, which is applied extrinsically (from the global frame).</param>
        /// <returns>The rotated pose.</returns>
        /// <remarks>
        /// This rotation is applied around the origin in the global frame.
        /// For rotations in the pose's local frame, use <see cref="TransformBy"/>.
        /// </remarks>
        public Pose3d RotateBy(Rotation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new Pose3d(Translation.RotateBy(other), Rotation.RotateBy(other));
        }

        /// <summary>
        /// Transforms the pose by the given transformation and returns the new transformed pose.
        /// </summary>
        /// <param name="other">The transform to transform the pose by.</param>
        /// <returns>The transformed pose.</returns>
        /// <remarks>
        /// The transform is applied relative to the pose's frame. Note that this differs from
        /// <see cref="RotateBy"/>, which is applied relative to the global frame and around the origin.
        /// </remarks>
        public Pose3d TransformBy(Transform3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new Pose3d(
                Translation.Plus(other.Translation.RotateBy(Rotation)),
                other.Rotation.Plus(Rotation)
            );
        }

        /// <summary>
        /// Returns the current pose relative to the given pose.
        /// </summary>
        /// <param name="other">The pose that is the origin of the new coordinate frame that the current pose will be converted into.</param>
        /// <returns>The current pose relative to the new origin pose.</returns>
        /// <remarks>
        /// This function can often be used for trajectory tracking or pose stabilization algorithms
        /// to get the error between the reference and the current pose.
        /// </remarks>
        public Pose3d RelativeTo(Pose3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var transform = new Transform3d(other, this);
            return new Pose3d(transform.Translation, transform.Rotation);
        }

        /// <summary>
        /// Rotates the current pose around a point in 3D space.
        /// </summary>
        /// <param name="point">The point in 3D space to rotate around.</param>
        /// <param name="rotation">The rotation to rotate the pose by.</param>
        /// <returns>The new rotated pose.</returns>
        public Pose3d RotateAround(Translation3d point, Rotation3d rotation)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));
            if (rotation == null)
                throw new ArgumentNullException(nameof(rotation));

            return new Pose3d(
                Translation.RotateAround(point, rotation),
                this.Rotation.RotateBy(rotation)
            );
        }

        /// <summary>
        /// Converts this pose to a 4x4 affine transformation matrix.
        /// </summary>
        /// <returns>A 4x4 affine transformation matrix representation of this pose.</returns>
        /// <remarks>
        /// The matrix is in the standard affine transformation format:
        /// [R | t]
        /// [0 | 1]
        /// where R is the 3x3 rotation matrix and t is the 3x1 translation vector.
        /// </remarks>
        public double[,] ToMatrix()
        {
            double[] vec = Translation.ToVector();
            double[,] mat = Rotation.ToMatrix();

            return new double[,]
            {
                { mat[0, 0], mat[0, 1], mat[0, 2], vec[0] },
                { mat[1, 0], mat[1, 1], mat[1, 2], vec[1] },
                { mat[2, 0], mat[2, 1], mat[2, 2], vec[2] },
                { 0.0, 0.0, 0.0, 1.0 },
            };
        }

        /// <summary>
        /// Returns the nearest Pose3d from a collection of poses.
        /// </summary>
        /// <param name="poses">The collection of poses to search.</param>
        /// <returns>The nearest Pose3d from the collection.</returns>
        /// <remarks>
        /// If two or more poses in the collection have the same distance from this pose,
        /// returns the one with the closest rotation component.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if poses is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
        public Pose3d Nearest(IEnumerable<Pose3d> poses)
        {
            if (poses == null)
                throw new ArgumentNullException(nameof(poses));

            return poses
                .OrderBy(p => Translation.GetDistance(p.Translation))
                .ThenBy(p => Rotation.Minus(p.Rotation).Angle)
                .First();
        }

        /// <summary>
        /// Returns a string representation of this pose.
        /// </summary>
        public override string ToString()
        {
            return $"Pose3d({Translation}, {Rotation})";
        }

        /// <summary>
        /// Checks equality between this Pose3d and another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is Pose3d other && Equals(other);
        }

        /// <summary>
        /// Checks equality between this Pose3d and another Pose3d.
        /// </summary>
        /// <param name="other">The other Pose3d.</param>
        /// <returns>True if the poses are equal, false otherwise.</returns>
        public bool Equals(Pose3d other)
        {
            if (other == null)
                return false;
            return Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
        }

        /// <summary>
        /// Returns a hash code for this pose.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Translation, Rotation);
        }

        /// <summary>
        /// Operator overload for pose transformation.
        /// </summary>
        public static Pose3d operator +(Pose3d pose, Transform3d transform) => pose.Plus(transform);

        /// <summary>
        /// Operator overload for computing the transform between poses.
        /// </summary>
        public static Transform3d operator -(Pose3d a, Pose3d b) => a.Minus(b);

        /// <summary>
        /// Operator overload for scalar multiplication.
        /// </summary>
        public static Pose3d operator *(Pose3d pose, double scalar) => pose.Times(scalar);

        /// <summary>
        /// Operator overload for scalar multiplication (reversed order).
        /// </summary>
        public static Pose3d operator *(double scalar, Pose3d pose) => pose.Times(scalar);

        /// <summary>
        /// Operator overload for scalar division.
        /// </summary>
        public static Pose3d operator /(Pose3d pose, double scalar) => pose.Div(scalar);

        /// <summary>
        /// Operator overload for equality comparison.
        /// </summary>
        public static bool operator ==(Pose3d a, Pose3d b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null || b is null)
                return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Operator overload for inequality comparison.
        /// </summary>
        public static bool operator !=(Pose3d a, Pose3d b) => !(a == b);
    }
}
