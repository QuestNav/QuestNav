using System;
using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a transformation for a Pose3d in the pose's frame.
    /// </summary>
    /// <remarks>
    /// A Transform3d consists of a translational and rotational component.
    /// It can be used to transform poses in 3D space, representing rigid body transformations.
    /// </remarks>
    public class Transform3d : IEquatable<Transform3d>
    {
        /// <summary>
        /// A preallocated Transform3d representing no transformation (identity transform).
        /// </summary>
        /// <remarks>
        /// This exists to avoid allocations for common transformations.
        /// </remarks>
        public static readonly Transform3d Zero = new Transform3d();

        /// <summary>
        /// Gets the translational component of the transform.
        /// </summary>
        [JsonProperty("translation")]
        public Translation3d Translation { get; }

        /// <summary>
        /// Gets the rotational component of the transform.
        /// </summary>
        [JsonProperty("rotation")]
        public Rotation3d Rotation { get; }

        /// <summary>
        /// Gets the X component of the transformation's translation.
        /// </summary>
        public double X => Translation.X;

        /// <summary>
        /// Gets the Y component of the transformation's translation.
        /// </summary>
        public double Y => Translation.Y;

        /// <summary>
        /// Gets the Z component of the transformation's translation.
        /// </summary>
        public double Z => Translation.Z;

        /// <summary>
        /// Constructs the identity transform that maps an initial pose to itself.
        /// </summary>
        public Transform3d()
        {
            Translation = Translation3d.Zero;
            Rotation = Rotation3d.Zero;
        }

        /// <summary>
        /// Constructs the transform that maps the initial pose to the final pose.
        /// </summary>
        /// <param name="initial">The initial pose for the transformation.</param>
        /// <param name="final">The final pose for the transformation.</param>
        /// <remarks>
        /// The transform is computed as the relative transformation needed to go from
        /// the initial pose to the final pose in the initial pose's reference frame.
        /// </remarks>
        public Transform3d(Pose3d initial, Pose3d final)
        {
            if (initial == null)
                throw new ArgumentNullException(nameof(initial));
            if (final == null)
                throw new ArgumentNullException(nameof(final));

            // We are rotating the difference between the translations
            // using a clockwise rotation matrix. This transforms the global
            // delta into a local delta (relative to the initial pose).
            Translation = final
                .Translation.Minus(initial.Translation)
                .RotateBy(initial.Rotation.UnaryMinus());

            Rotation = final.Rotation.Minus(initial.Rotation);
        }

        /// <summary>
        /// Constructs a transform with the given translation and rotation components.
        /// </summary>
        /// <param name="translation">The translational component of the transform.</param>
        /// <param name="rotation">The rotational component of the transform.</param>
        [JsonConstructor]
        public Transform3d(Translation3d translation, Rotation3d rotation)
        {
            this.Translation = translation ?? throw new ArgumentNullException(nameof(translation));
            this.Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        }

        /// <summary>
        /// Constructs a transform with x, y, and z translations instead of a separate Translation3d.
        /// </summary>
        /// <param name="x">The x component of the translational component of the transform.</param>
        /// <param name="y">The y component of the translational component of the transform.</param>
        /// <param name="z">The z component of the translational component of the transform.</param>
        /// <param name="rotation">The rotational component of the transform.</param>
        public Transform3d(double x, double y, double z, Rotation3d rotation)
        {
            Translation = new Translation3d(x, y, z);
            this.Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
        }

        /// <summary>
        /// Constructs a transform with the specified 4x4 affine transformation matrix.
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
        public Transform3d(double[,] matrix)
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
        /// Creates a new Transform3d from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf to convert</param>
        public Transform3d(ProtobufTransform3d protobuf)
        {
            Translation = new Translation3d(protobuf.Translation);
            Rotation = new Rotation3d(protobuf.Rotation);
        }

        /// <summary>
        /// Copies the data to a protobuf-sterilized version to be sent over NT
        /// </summary>
        /// <returns>The sterilized protobuf</returns>
        public ProtobufTransform3d ToProtobuf()
        {
            return new ProtobufTransform3d()
            {
                Translation = Translation.ToProtobuf(),
                Rotation = Rotation.ToProtobuf(),
            };
        }

        /// <summary>
        /// Multiplies the transform by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar multiplier.</param>
        /// <returns>The scaled Transform3d.</returns>
        /// <remarks>
        /// Both the translation and rotation components are scaled.
        /// Scaling the rotation uses spherical linear interpolation (SLERP).
        /// </remarks>
        public Transform3d Times(double scalar)
        {
            return new Transform3d(Translation.Times(scalar), Rotation.Times(scalar));
        }

        /// <summary>
        /// Divides the transform by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar divisor.</param>
        /// <returns>The scaled Transform3d.</returns>
        public Transform3d Div(double scalar)
        {
            return Times(1.0 / scalar);
        }

        /// <summary>
        /// Composes two transformations.
        /// </summary>
        /// <param name="other">The transform to compose with this one.</param>
        /// <returns>The composition of the two transformations.</returns>
        /// <remarks>
        /// The second transform is applied relative to the orientation of the first.
        /// This is equivalent to applying this transform, then applying the other transform
        /// in the resulting coordinate frame.
        /// </remarks>
        public Transform3d Plus(Transform3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new Transform3d(Pose3d.Zero, Pose3d.Zero.TransformBy(this).TransformBy(other));
        }

        /// <summary>
        /// Inverts this transformation.
        /// </summary>
        /// <returns>The inverted transformation.</returns>
        /// <remarks>
        /// This is useful for undoing a transformation. If transform T maps pose A to pose B,
        /// then T.Inverse() maps pose B back to pose A.
        /// </remarks>
        public Transform3d Inverse()
        {
            // We are rotating the difference between the translations
            // using a clockwise rotation matrix. This transforms the global
            // delta into a local delta (relative to the initial pose).
            return new Transform3d(
                Translation.UnaryMinus().RotateBy(Rotation.UnaryMinus()),
                Rotation.UnaryMinus()
            );
        }

        /// <summary>
        /// Converts this transform to a 4x4 affine transformation matrix.
        /// </summary>
        /// <returns>A 4x4 affine transformation matrix representation of this transform.</returns>
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
        /// Returns a string representation of this transform.
        /// </summary>
        public override string ToString()
        {
            return $"Transform3d({Translation}, {Rotation})";
        }

        /// <summary>
        /// Checks equality between this Transform3d and another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is Transform3d other && Equals(other);
        }

        /// <summary>
        /// Checks equality between this Transform3d and another Transform3d.
        /// </summary>
        /// <param name="other">The other Transform3d.</param>
        /// <returns>True if the transforms are equal, false otherwise.</returns>
        public bool Equals(Transform3d other)
        {
            if (other == null)
                return false;
            return Translation.Equals(other.Translation) && Rotation.Equals(other.Rotation);
        }

        /// <summary>
        /// Returns a hash code for this transform.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Translation, Rotation);
        }

        /// <summary>
        /// Operator overload for transform composition.
        /// </summary>
        public static Transform3d operator +(Transform3d a, Transform3d b) => a.Plus(b);

        /// <summary>
        /// Operator overload for scalar multiplication.
        /// </summary>
        public static Transform3d operator *(Transform3d transform, double scalar) =>
            transform.Times(scalar);

        /// <summary>
        /// Operator overload for scalar multiplication (reversed order).
        /// </summary>
        public static Transform3d operator *(double scalar, Transform3d transform) =>
            transform.Times(scalar);

        /// <summary>
        /// Operator overload for scalar division.
        /// </summary>
        public static Transform3d operator /(Transform3d transform, double scalar) =>
            transform.Div(scalar);

        /// <summary>
        /// Operator overload for equality comparison.
        /// </summary>
        public static bool operator ==(Transform3d a, Transform3d b)
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
        public static bool operator !=(Transform3d a, Transform3d b) => !(a == b);
    }
}
