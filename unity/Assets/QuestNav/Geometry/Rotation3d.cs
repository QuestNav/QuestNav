using System;
using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a rotation in a 3D coordinate frame using a quaternion representation.
    /// </summary>
    /// <remarks>
    /// This class provides multiple ways to represent and manipulate 3D rotations:
    /// - Quaternion (internal representation)
    /// - Euler angles (roll, pitch, yaw)
    /// - Axis-angle representation
    /// - Rotation matrix
    /// - Rotation vector
    ///
    /// All angles are measured in radians and follow the right-hand rule:
    /// pointing your right thumb along the positive axis direction, your fingers
    /// curl in the direction of positive rotation.
    /// </remarks>
    public class Rotation3d : IEquatable<Rotation3d>
    {
        /// <summary>
        /// A preallocated Rotation3d representing no rotation (identity).
        /// </summary>
        /// <remarks>
        /// This exists to avoid allocations for common rotations.
        /// </remarks>
        public static readonly Rotation3d Zero = new Rotation3d();

        /// <summary>
        /// Gets the quaternion representation of this Rotation3d.
        /// </summary>
        [JsonProperty("quaternion")]
        public Quaternion Quaternion { get; }

        /// <summary>
        /// Gets the counterclockwise rotation angle around the X axis (roll) in radians.
        /// </summary>
        public double X
        {
            get
            {
                double w = Quaternion.W;
                double x = Quaternion.X;
                double y = Quaternion.Y;
                double z = Quaternion.Z;

                // wpimath/algorithms.md
                double cxcy = 1.0 - 2.0 * (x * x + y * y);
                double sxcy = 2.0 * (w * x + y * z);
                double cySquared = cxcy * cxcy + sxcy * sxcy;

                if (cySquared > 1e-20)
                {
                    return Math.Atan2(sxcy, cxcy);
                }
                else
                {
                    return 0.0;
                }
            }
        }

        /// <summary>
        /// Gets the counterclockwise rotation angle around the Y axis (pitch) in radians.
        /// </summary>
        public double Y
        {
            get
            {
                double w = Quaternion.W;
                double x = Quaternion.X;
                double y = Quaternion.Y;
                double z = Quaternion.Z;

                // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Quaternion_to_Euler_angles_(in_3-2-1_sequence)_conversion
                double ratio = 2.0 * (w * y - z * x);

                if (Math.Abs(ratio) >= 1.0)
                {
                    return (ratio >= 0 ? 1 : -1) * Math.PI / 2.0;
                }
                else
                {
                    return Math.Asin(ratio);
                }
            }
        }

        /// <summary>
        /// Gets the counterclockwise rotation angle around the Z axis (yaw) in radians.
        /// </summary>
        public double Z
        {
            get
            {
                double w = Quaternion.W;
                double x = Quaternion.X;
                double y = Quaternion.Y;
                double z = Quaternion.Z;

                // wpimath/algorithms.md
                double cycz = 1.0 - 2.0 * (y * y + z * z);
                double cysz = 2.0 * (w * z + x * y);
                double cySquared = cycz * cycz + cysz * cysz;

                if (cySquared > 1e-20)
                {
                    return Math.Atan2(cysz, cycz);
                }
                else
                {
                    return Math.Atan2(2.0 * w * z, w * w - z * z);
                }
            }
        }

        /// <summary>
        /// Gets the rotation axis in the axis-angle representation.
        /// </summary>
        /// <returns>The normalized rotation axis as a 3-element array [x, y, z].</returns>
        public double[] Axis
        {
            get
            {
                double norm = Math.Sqrt(
                    Quaternion.X * Quaternion.X
                        + Quaternion.Y * Quaternion.Y
                        + Quaternion.Z * Quaternion.Z
                );

                if (norm == 0.0)
                {
                    return new double[] { 0.0, 0.0, 0.0 };
                }
                else
                {
                    return new double[]
                    {
                        Quaternion.X / norm,
                        Quaternion.Y / norm,
                        Quaternion.Z / norm,
                    };
                }
            }
        }

        /// <summary>
        /// Gets the rotation angle in radians in the axis-angle representation.
        /// </summary>
        public double Angle
        {
            get
            {
                double norm = Math.Sqrt(
                    Quaternion.X * Quaternion.X
                        + Quaternion.Y * Quaternion.Y
                        + Quaternion.Z * Quaternion.Z
                );
                return 2.0 * Math.Atan2(norm, Quaternion.W);
            }
        }

        /// <summary>
        /// Constructs a Rotation3d representing no rotation (identity rotation).
        /// </summary>
        public Rotation3d()
        {
            Quaternion = new Quaternion();
        }

        /// <summary>
        /// Constructs a Rotation3d from a quaternion.
        /// </summary>
        /// <param name="q">The quaternion. Will be automatically normalized.</param>
        [JsonConstructor]
        public Rotation3d(Quaternion quaternion)
        {
            Quaternion = quaternion.Normalize();
        }

        /// <summary>
        /// Constructs a Rotation3d from extrinsic roll, pitch, and yaw angles.
        /// </summary>
        /// <param name="roll">The counterclockwise rotation angle around the X axis (roll) in radians.</param>
        /// <param name="pitch">The counterclockwise rotation angle around the Y axis (pitch) in radians.</param>
        /// <param name="yaw">The counterclockwise rotation angle around the Z axis (yaw) in radians.</param>
        /// <remarks>
        /// Extrinsic rotations occur in that order around the axes in the fixed global frame
        /// rather than the body frame.
        ///
        /// Angles are measured counterclockwise with the rotation axis pointing "out of the page".
        /// If you point your right thumb along the positive axis direction, your fingers curl
        /// in the direction of positive rotation.
        /// </remarks>
        public Rotation3d(double roll, double pitch, double yaw)
        {
            // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Euler_angles_to_quaternion_conversion
            double cr = Math.Cos(roll * 0.5);
            double sr = Math.Sin(roll * 0.5);

            double cp = Math.Cos(pitch * 0.5);
            double sp = Math.Sin(pitch * 0.5);

            double cy = Math.Cos(yaw * 0.5);
            double sy = Math.Sin(yaw * 0.5);

            Quaternion = new Quaternion(
                cr * cp * cy + sr * sp * sy,
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy
            );
        }

        /// <summary>
        /// Constructs a Rotation3d from a rotation vector.
        /// </summary>
        /// <param name="rvec">The rotation vector as a 3-element array [x, y, z].</param>
        /// <remarks>
        /// This representation is equivalent to axis-angle, where the normalized axis
        /// is multiplied by the rotation angle around the axis in radians.
        /// The magnitude of the vector is the rotation angle, and the direction is the rotation axis.
        /// </remarks>
        public Rotation3d(double[] rvec)
        {
            if (rvec == null || rvec.Length != 3)
                throw new ArgumentException(
                    "Rotation vector must have exactly 3 elements",
                    nameof(rvec)
                );

            double norm = Math.Sqrt(rvec[0] * rvec[0] + rvec[1] * rvec[1] + rvec[2] * rvec[2]);

            if (norm == 0.0)
            {
                Quaternion = new Quaternion();
                return;
            }

            // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Definition
            double halfAngle = norm / 2.0;
            double sinHalfAngle = Math.Sin(halfAngle);
            double scale = sinHalfAngle / norm;

            Quaternion = new Quaternion(
                Math.Cos(halfAngle),
                rvec[0] * scale,
                rvec[1] * scale,
                rvec[2] * scale
            );
        }

        /// <summary>
        /// Constructs a Rotation3d from an axis-angle representation.
        /// </summary>
        /// <param name="axis">The rotation axis as a 3-element array [x, y, z]. Does not need to be normalized.</param>
        /// <param name="angleRadians">The rotation angle around the axis in radians.</param>
        public Rotation3d(double[] axis, double angleRadians)
        {
            if (axis == null || axis.Length != 3)
                throw new ArgumentException("Axis must have exactly 3 elements", nameof(axis));

            double norm = Math.Sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);

            if (norm == 0.0)
            {
                Quaternion = new Quaternion();
                return;
            }

            // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Definition
            double halfAngle = angleRadians / 2.0;
            double sinHalfAngle = Math.Sin(halfAngle);
            double scale = sinHalfAngle / norm;

            Quaternion = new Quaternion(
                Math.Cos(halfAngle),
                axis[0] * scale,
                axis[1] * scale,
                axis[2] * scale
            );
        }

        /// <summary>
        /// Constructs a Rotation3d from a 3x3 rotation matrix.
        /// </summary>
        /// <param name="rotationMatrix">The 3x3 rotation matrix as a 2D array.</param>
        /// <exception cref="ArgumentException">Thrown if the matrix isn't special orthogonal (SO(3)).</exception>
        /// <remarks>
        /// The matrix must be special orthogonal, meaning:
        /// - Orthogonal: R * R^T = I
        /// - Normalized: det(R) = 1
        /// </remarks>
        public Rotation3d(double[,] rotationMatrix)
        {
            if (
                rotationMatrix == null
                || rotationMatrix.GetLength(0) != 3
                || rotationMatrix.GetLength(1) != 3
            )
                throw new ArgumentException("Rotation matrix must be 3x3", nameof(rotationMatrix));

            // Verify that the matrix is orthogonal (R * R^T = I)
            double[,] product = MultiplyMatrices(rotationMatrix, TransposeMatrix(rotationMatrix));
            double[,] identity =
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 },
            };

            if (FrobeniusNorm(SubtractMatrices(product, identity)) > 1e-9)
            {
                throw new ArgumentException("Rotation matrix isn't orthogonal");
            }

            // Verify that the determinant is 1
            if (Math.Abs(Determinant3x3(rotationMatrix) - 1.0) > 1e-9)
            {
                throw new ArgumentException(
                    "Rotation matrix is orthogonal but not special orthogonal (det != 1)"
                );
            }

            // Convert rotation matrix to quaternion
            // https://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
            double trace = rotationMatrix[0, 0] + rotationMatrix[1, 1] + rotationMatrix[2, 2];
            double w,
                x,
                y,
                z;

            if (trace > 0.0)
            {
                double s = 0.5 / Math.Sqrt(trace + 1.0);
                w = 0.25 / s;
                x = (rotationMatrix[2, 1] - rotationMatrix[1, 2]) * s;
                y = (rotationMatrix[0, 2] - rotationMatrix[2, 0]) * s;
                z = (rotationMatrix[1, 0] - rotationMatrix[0, 1]) * s;
            }
            else
            {
                if (
                    rotationMatrix[0, 0] > rotationMatrix[1, 1]
                    && rotationMatrix[0, 0] > rotationMatrix[2, 2]
                )
                {
                    double s =
                        2.0
                        * Math.Sqrt(
                            1.0 + rotationMatrix[0, 0] - rotationMatrix[1, 1] - rotationMatrix[2, 2]
                        );
                    w = (rotationMatrix[2, 1] - rotationMatrix[1, 2]) / s;
                    x = 0.25 * s;
                    y = (rotationMatrix[0, 1] + rotationMatrix[1, 0]) / s;
                    z = (rotationMatrix[0, 2] + rotationMatrix[2, 0]) / s;
                }
                else if (rotationMatrix[1, 1] > rotationMatrix[2, 2])
                {
                    double s =
                        2.0
                        * Math.Sqrt(
                            1.0 + rotationMatrix[1, 1] - rotationMatrix[0, 0] - rotationMatrix[2, 2]
                        );
                    w = (rotationMatrix[0, 2] - rotationMatrix[2, 0]) / s;
                    x = (rotationMatrix[0, 1] + rotationMatrix[1, 0]) / s;
                    y = 0.25 * s;
                    z = (rotationMatrix[1, 2] + rotationMatrix[2, 1]) / s;
                }
                else
                {
                    double s =
                        2.0
                        * Math.Sqrt(
                            1.0 + rotationMatrix[2, 2] - rotationMatrix[0, 0] - rotationMatrix[1, 1]
                        );
                    w = (rotationMatrix[1, 0] - rotationMatrix[0, 1]) / s;
                    x = (rotationMatrix[0, 2] + rotationMatrix[2, 0]) / s;
                    y = (rotationMatrix[1, 2] + rotationMatrix[2, 1]) / s;
                    z = 0.25 * s;
                }
            }

            Quaternion = new Quaternion(w, x, y, z);
        }

        /// <summary>
        /// Constructs a Rotation3d that rotates the initial vector onto the final vector.
        /// </summary>
        /// <param name="initial">The initial vector as a 3-element array [x, y, z].</param>
        /// <param name="final">The final vector as a 3-element array [x, y, z].</param>
        /// <remarks>
        /// This is useful for turning a 3D vector (final) into an orientation relative to
        /// a coordinate system vector (initial).
        /// </remarks>
        public Rotation3d(double[] initial, double[] final)
        {
            if (initial == null || initial.Length != 3)
                throw new ArgumentException(
                    "Initial vector must have exactly 3 elements",
                    nameof(initial)
                );
            if (final == null || final.Length != 3)
                throw new ArgumentException(
                    "Final vector must have exactly 3 elements",
                    nameof(final)
                );

            double dot = Dot(initial, final);
            double normProduct = Norm(initial) * Norm(final);
            double dotNorm = dot / normProduct;

            if (dotNorm > 1.0 - 1e-9)
            {
                // Vectors point in the same direction - no rotation needed
                Quaternion = new Quaternion();
            }
            else if (dotNorm < -1.0 + 1e-9)
            {
                // Vectors are antiparallel - 180° rotation required
                // Find the most orthogonal axis
                double x = Math.Abs(initial[0]);
                double y = Math.Abs(initial[1]);
                double z = Math.Abs(initial[2]);

                double[] other;
                if (x < y)
                {
                    other = (x < z) ? new double[] { 1, 0, 0 } : new double[] { 0, 0, 1 };
                }
                else
                {
                    other = (y < z) ? new double[] { 0, 1, 0 } : new double[] { 0, 0, 1 };
                }

                double[] axis = Cross(initial, other);
                double axisNorm = Norm(axis);

                Quaternion = new Quaternion(
                    0.0,
                    axis[0] / axisNorm,
                    axis[1] / axisNorm,
                    axis[2] / axisNorm
                );
            }
            else
            {
                double[] axis = Cross(initial, final);
                // https://stackoverflow.com/a/11741520
                Quaternion = new Quaternion(
                    normProduct + dot,
                    axis[0],
                    axis[1],
                    axis[2]
                ).Normalize();
            }
        }

        /// <summary>
        /// Creates a new Rotation3d from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf to convert</param>
        public Rotation3d(ProtobufRotation3d protobuf)
        {
            Quaternion = new Quaternion(protobuf.Q);
        }

        /// <summary>
        /// Converts the Rotation3d to a protobuf
        /// </summary>
        /// <returns>A protobuf serialized version</returns>
        public ProtobufRotation3d ToProtobuf()
        {
            var proto = new ProtobufRotation3d { Q = Quaternion.ToProtobuf() };
            return proto;
        }

        /// <summary>
        /// Adds two rotations together.
        /// </summary>
        /// <param name="other">The rotation to add.</param>
        /// <returns>The sum of the two rotations.</returns>
        public Rotation3d Plus(Rotation3d other)
        {
            return RotateBy(other);
        }

        /// <summary>
        /// Subtracts the other rotation from the current rotation.
        /// </summary>
        /// <param name="other">The rotation to subtract.</param>
        /// <returns>The difference between the two rotations.</returns>
        public Rotation3d Minus(Rotation3d other)
        {
            return RotateBy(other.UnaryMinus());
        }

        /// <summary>
        /// Returns the inverse of the current rotation.
        /// </summary>
        /// <returns>The inverse of the current rotation.</returns>
        public Rotation3d UnaryMinus()
        {
            return new Rotation3d(Quaternion.Inverse());
        }

        /// <summary>
        /// Multiplies the current rotation by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar multiplier.</param>
        /// <returns>The scaled rotation.</returns>
        /// <remarks>
        /// This uses spherical linear interpolation (SLERP) to scale the rotation.
        /// </remarks>
        public Rotation3d Times(double scalar)
        {
            // https://en.wikipedia.org/wiki/Slerp#Quaternion_Slerp
            if (Quaternion.W >= 0.0)
            {
                return new Rotation3d(
                    new double[] { Quaternion.X, Quaternion.Y, Quaternion.Z },
                    2.0 * scalar * Math.Acos(Quaternion.W)
                );
            }
            else
            {
                return new Rotation3d(
                    new double[] { -Quaternion.X, -Quaternion.Y, -Quaternion.Z },
                    2.0 * scalar * Math.Acos(-Quaternion.W)
                );
            }
        }

        /// <summary>
        /// Divides the current rotation by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar divisor.</param>
        /// <returns>The scaled rotation.</returns>
        public Rotation3d Div(double scalar)
        {
            return Times(1.0 / scalar);
        }

        /// <summary>
        /// Applies an extrinsic rotation to this rotation.
        /// </summary>
        /// <param name="other">The extrinsic rotation to apply.</param>
        /// <returns>The new rotated Rotation3d.</returns>
        /// <remarks>
        /// The other rotation is applied extrinsically, which means it rotates around
        /// the global axes. For example, new Rotation3d(π/2, 0, 0).RotateBy(new Rotation3d(0, π/4, 0))
        /// rotates by 90° around the +X axis and then by 45° around the global +Y axis.
        /// </remarks>
        public Rotation3d RotateBy(Rotation3d other)
        {
            return new Rotation3d(other.Quaternion * Quaternion);
        }

        /// <summary>
        /// Converts this rotation to a 3x3 rotation matrix.
        /// </summary>
        /// <returns>The rotation matrix as a 3x3 2D array.</returns>
        /// <remarks>
        /// Uses the formula from https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation#Quaternion-derived_rotation_matrix
        /// </remarks>
        public double[,] ToMatrix()
        {
            double w = Quaternion.W;
            double x = Quaternion.X;
            double y = Quaternion.Y;
            double z = Quaternion.Z;

            return new double[,]
            {
                { 1.0 - 2.0 * (y * y + z * z), 2.0 * (x * y - w * z), 2.0 * (x * z + w * y) },
                { 2.0 * (x * y + w * z), 1.0 - 2.0 * (x * x + z * z), 2.0 * (y * z - w * x) },
                { 2.0 * (x * z - w * y), 2.0 * (y * z + w * x), 1.0 - 2.0 * (x * x + y * y) },
            };
        }

        /// <summary>
        /// Converts this rotation to a rotation vector representation.
        /// </summary>
        /// <returns>The rotation vector as a 3-element array [x, y, z].</returns>
        /// <remarks>
        /// The rotation vector's magnitude is the rotation angle in radians,
        /// and its direction is the rotation axis.
        /// </remarks>
        public double[] ToVector()
        {
            return Quaternion.ToRotationVector();
        }

        /// <summary>
        /// Returns a string representation of this rotation.
        /// </summary>
        public override string ToString()
        {
            return $"Rotation3d({Quaternion})";
        }

        /// <summary>
        /// Checks equality between this Rotation3d and another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects represent the same rotation, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is Rotation3d other && Equals(other);
        }

        /// <summary>
        /// Checks equality between this Rotation3d and another Rotation3d.
        /// </summary>
        /// <param name="other">The other Rotation3d.</param>
        /// <returns>True if the rotations are equivalent, false otherwise.</returns>
        /// <remarks>
        /// Two rotations are considered equal if their quaternions represent the same orientation,
        /// accounting for the double-cover property of quaternions (q and -q represent the same rotation).
        /// </remarks>
        public bool Equals(Rotation3d other)
        {
            if (other == null)
                return false;
            return Math.Abs(
                    Math.Abs(Quaternion.Dot(other.Quaternion))
                        - Quaternion.Norm() * other.Quaternion.Norm()
                ) < 1e-9;
        }

        /// <summary>
        /// Returns a hash code for this rotation.
        /// </summary>
        public override int GetHashCode()
        {
            return Quaternion.GetHashCode();
        }

        /// <summary>
        /// Interpolates between this rotation and another rotation.
        /// </summary>
        /// <param name="endValue">The end rotation to interpolate to.</param>
        /// <param name="t">The interpolation parameter between 0 and 1.</param>
        /// <returns>The interpolated rotation.</returns>
        /// <remarks>
        /// This uses spherical linear interpolation (SLERP).
        /// The parameter t is clamped to the range [0, 1].
        /// </remarks>
        public Rotation3d Interpolate(Rotation3d endValue, double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);
            return Plus(endValue.Minus(this).Times(t));
        }

        /// <summary>
        /// Operator overload for rotation addition.
        /// </summary>
        public static Rotation3d operator +(Rotation3d a, Rotation3d b) => a.Plus(b);

        /// <summary>
        /// Operator overload for rotation subtraction.
        /// </summary>
        public static Rotation3d operator -(Rotation3d a, Rotation3d b) => a.Minus(b);

        /// <summary>
        /// Operator overload for unary negation (inverse).
        /// </summary>
        public static Rotation3d operator -(Rotation3d a) => a.UnaryMinus();

        /// <summary>
        /// Operator overload for scalar multiplication.
        /// </summary>
        public static Rotation3d operator *(Rotation3d rotation, double scalar) =>
            rotation.Times(scalar);

        /// <summary>
        /// Operator overload for scalar multiplication (reversed order).
        /// </summary>
        public static Rotation3d operator *(double scalar, Rotation3d rotation) =>
            rotation.Times(scalar);

        /// <summary>
        /// Operator overload for scalar division.
        /// </summary>
        public static Rotation3d operator /(Rotation3d rotation, double scalar) =>
            rotation.Div(scalar);

        /// <summary>
        /// Operator overload for equality comparison.
        /// </summary>
        public static bool operator ==(Rotation3d a, Rotation3d b)
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
        public static bool operator !=(Rotation3d a, Rotation3d b) => !(a == b);

        // Helper methods for vector operations
        private static double Dot(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }

        private static double Norm(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }

        private static double[] Cross(double[] a, double[] b)
        {
            return new double[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0],
            };
        }

        // Helper methods for matrix operations
        private static double[,] MultiplyMatrices(double[,] a, double[,] b)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            for (int k = 0; k < 3; k++)
                result[i, j] += a[i, k] * b[k, j];
            return result;
        }

        private static double[,] TransposeMatrix(double[,] matrix)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                result[i, j] = matrix[j, i];
            return result;
        }

        private static double[,] SubtractMatrices(double[,] a, double[,] b)
        {
            double[,] result = new double[3, 3];
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                result[i, j] = a[i, j] - b[i, j];
            return result;
        }

        private static double FrobeniusNorm(double[,] matrix)
        {
            double sum = 0.0;
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                sum += matrix[i, j] * matrix[i, j];
            return Math.Sqrt(sum);
        }

        private static double Determinant3x3(double[,] m)
        {
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
                - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
                + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        }
    }
}
