using System;
using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a quaternion for 3D rotations.
    /// </summary>
    /// <remarks>
    /// A quaternion is a mathematical structure used to represent rotations in 3D space.
    /// It consists of four components: a scalar part (W) and a vector part (X, Y, Z).
    /// Quaternions avoid gimbal lock and provide smooth interpolation between rotations.
    /// </remarks>
    public class Quaternion : IEquatable<Quaternion>
    {
        /// <summary>
        /// Gets the scalar (W) component of the quaternion in versor form.
        /// </summary>
        [JsonProperty("W")]
        public double W { get; }

        /// <summary>
        /// Gets the X component of the vector part in versor form.
        /// </summary>
        [JsonProperty("X")]
        public double X { get; }

        /// <summary>
        /// Gets the Y component of the vector part in versor form.
        /// </summary>
        [JsonProperty("Y")]
        public double Y { get; }

        /// <summary>
        /// Gets the Z component of the vector part in versor form.
        /// </summary>
        [JsonProperty("Z")]
        public double Z { get; }

        /// <summary>
        /// Constructs an identity quaternion with a default rotation of 0 degrees.
        /// </summary>
        public Quaternion()
        {
            W = 1.0;
            X = 0.0;
            Y = 0.0;
            Z = 0.0;
        }

        /// <summary>
        /// Constructs a quaternion with the given components.
        /// </summary>
        /// <param name="w">The scalar (W) component of the quaternion.</param>
        /// <param name="x">The X component of the quaternion.</param>
        /// <param name="y">The Y component of the quaternion.</param>
        /// <param name="z">The Z component of the quaternion.</param>
        [JsonConstructor]
        public Quaternion(double w, double x, double y, double z)
        {
            W = w;
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Builds a new Quaternion from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf quaternion to copy</param>
        public Quaternion(ProtobufQuaternion protobuf)
        {
            X = protobuf.X;
            Y = protobuf.Y;
            Z = protobuf.Z;
            W = protobuf.W;
        }

        /// <summary>
        /// Converts the Quaternion to a ProtobufQuaternion
        /// </summary>
        /// <returns>A protobuf holding the quaternion</returns>
        public ProtobufQuaternion ToProtobuf()
        {
            var proto = new ProtobufQuaternion
            {
                X = X,
                Y = Y,
                Z = Z,
                W = W,
            };
            return proto;
        }

        /// <summary>
        /// Adds another quaternion to this quaternion component-wise.
        /// </summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns>The quaternion sum.</returns>
        public Quaternion Plus(Quaternion other)
        {
            return new Quaternion(W + other.W, X + other.X, Y + other.Y, Z + other.Z);
        }

        /// <summary>
        /// Subtracts another quaternion from this quaternion component-wise.
        /// </summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns>The quaternion difference.</returns>
        public Quaternion Minus(Quaternion other)
        {
            return new Quaternion(W - other.W, X - other.X, Y - other.Y, Z - other.Z);
        }

        /// <summary>
        /// Divides each component by a scalar value.
        /// </summary>
        /// <param name="scalar">The value to divide each component by.</param>
        /// <returns>The scaled quaternion.</returns>
        public Quaternion Divide(double scalar)
        {
            return new Quaternion(W / scalar, X / scalar, Y / scalar, Z / scalar);
        }

        /// <summary>
        /// Multiplies each component by a scalar value.
        /// </summary>
        /// <param name="scalar">The value to multiply each component by.</param>
        /// <returns>The scaled quaternion.</returns>
        public Quaternion Times(double scalar)
        {
            return new Quaternion(W * scalar, X * scalar, Y * scalar, Z * scalar);
        }

        /// <summary>
        /// Multiplies this quaternion with another quaternion (Hamilton product).
        /// </summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns>The quaternion product.</returns>
        /// <remarks>
        /// Uses the formula from https://en.wikipedia.org/wiki/Quaternion#Scalar_and_vector_parts
        /// where q₁ * q₂ = (r₁r₂ - v₁·v₂, r₁v₂ + r₂v₁ + v₁×v₂)
        /// </remarks>
        public Quaternion Times(Quaternion other)
        {
            double r1 = W;
            double r2 = other.W;

            // v₁ · v₂ (dot product)
            double dot = X * other.X + Y * other.Y + Z * other.Z;

            // v₁ × v₂ (cross product)
            double crossX = Y * other.Z - other.Y * Z;
            double crossY = other.X * Z - X * other.Z;
            double crossZ = X * other.Y - other.X * Y;

            return new Quaternion(
                // r = r₁r₂ − v₁ · v₂
                r1 * r2
                    - dot,
                // v = r₁v₂ + r₂v₁ + v₁ × v₂
                r1 * other.X
                    + r2 * X
                    + crossX,
                r1 * other.Y + r2 * Y + crossY,
                r1 * other.Z + r2 * Z + crossZ
            );
        }

        /// <summary>
        /// Returns a string representation of the quaternion.
        /// </summary>
        /// <returns>A string in the format "Quaternion(w, x, y, z)".</returns>
        public override string ToString()
        {
            return $"Quaternion({W}, {X}, {Y}, {Z})";
        }

        /// <summary>
        /// Checks equality between this quaternion and another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects represent the same rotation, false otherwise.</returns>
        /// <remarks>
        /// Two quaternions are considered equal if they represent the same rotation,
        /// accounting for numerical precision (tolerance of 1e-9).
        /// </remarks>
        public override bool Equals(object obj)
        {
            return obj is Quaternion other && Equals(other);
        }

        /// <summary>
        /// Checks equality between this quaternion and another quaternion.
        /// </summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns>True if the quaternions represent the same rotation, false otherwise.</returns>
        public bool Equals(Quaternion other)
        {
            if (other == null)
                return false;
            return Math.Abs(Dot(other) - Norm() * other.Norm()) < 1e-9
                && Math.Abs(Norm() - other.Norm()) < 1e-9;
        }

        /// <summary>
        /// Returns a hash code for this quaternion.
        /// </summary>
        /// <returns>A hash code value.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(W, X, Y, Z);
        }

        /// <summary>
        /// Returns the conjugate of the quaternion.
        /// </summary>
        /// <returns>The conjugate quaternion (W, -X, -Y, -Z).</returns>
        /// <remarks>
        /// The conjugate of a quaternion q = (w, x, y, z) is q* = (w, -x, -y, -z).
        /// For unit quaternions, the conjugate equals the inverse.
        /// </remarks>
        public Quaternion Conjugate()
        {
            return new Quaternion(W, -X, -Y, -Z);
        }

        /// <summary>
        /// Returns the dot product of this quaternion with another quaternion.
        /// </summary>
        /// <param name="other">The other quaternion.</param>
        /// <returns>The dot product (scalar value).</returns>
        /// <remarks>
        /// The dot product is computed as: w₁w₂ + x₁x₂ + y₁y₂ + z₁z₂
        /// </remarks>
        public double Dot(Quaternion other)
        {
            return W * other.W + X * other.X + Y * other.Y + Z * other.Z;
        }

        /// <summary>
        /// Returns the inverse of the quaternion.
        /// </summary>
        /// <returns>The inverse quaternion.</returns>
        /// <remarks>
        /// The inverse is calculated as q⁻¹ = q* / |q|²,
        /// where q* is the conjugate and |q| is the norm.
        /// </remarks>
        public Quaternion Inverse()
        {
            double norm = Norm();
            return Conjugate().Divide(norm * norm);
        }

        /// <summary>
        /// Calculates the L2 norm (magnitude) of the quaternion.
        /// </summary>
        /// <returns>The L2 norm.</returns>
        /// <remarks>
        /// The norm is calculated as √(w² + x² + y² + z²).
        /// For unit quaternions representing rotations, the norm is 1.
        /// </remarks>
        public double Norm()
        {
            return Math.Sqrt(Dot(this));
        }

        /// <summary>
        /// Returns a normalized version of this quaternion.
        /// </summary>
        /// <returns>The normalized quaternion with unit length.</returns>
        /// <remarks>
        /// If the quaternion has zero norm, returns an identity quaternion.
        /// Otherwise, divides each component by the norm.
        /// </remarks>
        public Quaternion Normalize()
        {
            double norm = Norm();
            if (norm == 0.0)
            {
                return new Quaternion();
            }
            else
            {
                return new Quaternion(W / norm, X / norm, Y / norm, Z / norm);
            }
        }

        /// <summary>
        /// Raises this quaternion to a rational power.
        /// </summary>
        /// <param name="t">The power to raise this quaternion to.</param>
        /// <returns>The quaternion raised to the power t.</returns>
        /// <remarks>
        /// Computed as q^t = exp(t * log(q)).
        /// </remarks>
        public Quaternion Pow(double t)
        {
            // q^t = e^(ln(q^t)) = e^(t * ln(q))
            return Log().Times(t).Exp();
        }

        /// <summary>
        /// Applies a twist (adjustment) to this quaternion using matrix exponential.
        /// </summary>
        /// <param name="adjustment">The "twist" quaternion to apply.</param>
        /// <returns>The quaternion product of exp(adjustment) * this.</returns>
        public Quaternion Exp(Quaternion adjustment)
        {
            return adjustment.Exp().Times(this);
        }

        /// <summary>
        /// Computes the matrix exponential of this quaternion.
        /// </summary>
        /// <returns>The matrix exponential of this quaternion.</returns>
        /// <remarks>
        /// Source: wpimath/algorithms.md
        /// If this quaternion is in 𝖘𝖔(3) and you are looking for an element of SO(3),
        /// use <see cref="FromRotationVector"/> instead.
        /// Uses Taylor series approximation for small magnitudes to maintain numerical stability.
        /// </remarks>
        public Quaternion Exp()
        {
            double scalar = Math.Exp(W);

            double axialMagnitude = Math.Sqrt(X * X + Y * Y + Z * Z);
            double cosine = Math.Cos(axialMagnitude);

            double axialScalar;

            if (axialMagnitude < 1e-9)
            {
                // Taylor series of sin(θ) / θ near θ = 0: 1 − θ²/6 + θ⁴/120 + O(n⁶)
                double axialMagnitudeSq = axialMagnitude * axialMagnitude;
                double axialMagnitudeSqSq = axialMagnitudeSq * axialMagnitudeSq;
                axialScalar = 1.0 - axialMagnitudeSq / 6.0 + axialMagnitudeSqSq / 120.0;
            }
            else
            {
                axialScalar = Math.Sin(axialMagnitude) / axialMagnitude;
            }

            return new Quaternion(
                cosine * scalar,
                X * axialScalar * scalar,
                Y * axialScalar * scalar,
                Z * axialScalar * scalar
            );
        }

        /// <summary>
        /// Computes the logarithm mapping from this quaternion to another quaternion.
        /// </summary>
        /// <param name="end">The quaternion to map this quaternion onto.</param>
        /// <returns>The "twist" that maps this quaternion to the target quaternion.</returns>
        public Quaternion Log(Quaternion end)
        {
            return end.Times(Inverse()).Log();
        }

        /// <summary>
        /// Computes the logarithm of this quaternion.
        /// </summary>
        /// <returns>The logarithm of this quaternion.</returns>
        /// <remarks>
        /// Source: wpimath/algorithms.md
        /// If this quaternion is in SO(3) and you are looking for an element of 𝖘𝖔(3),
        /// use <see cref="ToRotationVector"/> instead.
        /// Uses Taylor series approximation for small magnitudes to maintain numerical stability.
        /// </remarks>
        public Quaternion Log()
        {
            double norm = Norm();
            double scalar = Math.Log(norm);

            double vNorm = Math.Sqrt(X * X + Y * Y + Z * Z);
            double sNorm = W / norm;

            if (Math.Abs(sNorm + 1) < 1e-9)
            {
                return new Quaternion(scalar, -Math.PI, 0, 0);
            }

            double vScalar;

            if (vNorm < 1e-9)
            {
                // Taylor series expansion of atan2(y/x)/y at y = 0:
                // 1/x - 1/3 y²/x³ + O(y⁴)
                vScalar = 1.0 / W - 1.0 / 3.0 * vNorm * vNorm / (W * W * W);
            }
            else
            {
                vScalar = Math.Atan2(vNorm, W) / vNorm;
            }

            return new Quaternion(scalar, vScalar * X, vScalar * Y, vScalar * Z);
        }

        /// <summary>
        /// Creates a quaternion from a rotation vector (axis-angle representation).
        /// </summary>
        /// <param name="rvec">The rotation vector as a 3-element array [x, y, z].</param>
        /// <returns>The quaternion representation of the rotation vector.</returns>
        /// <remarks>
        /// This is also the exponential map (exp operator) of 𝖘𝖔(3).
        /// Source: wpimath/algorithms.md
        /// The rotation vector's magnitude represents the rotation angle in radians,
        /// and its direction represents the rotation axis.
        /// Uses Taylor series approximation for small angles to maintain numerical stability.
        /// </remarks>
        public static Quaternion FromRotationVector(double[] rvec)
        {
            if (rvec == null || rvec.Length != 3)
                throw new ArgumentException(
                    "Rotation vector must have exactly 3 elements",
                    nameof(rvec)
                );

            double theta = Math.Sqrt(rvec[0] * rvec[0] + rvec[1] * rvec[1] + rvec[2] * rvec[2]);
            double cos = Math.Cos(theta / 2.0);

            double axialScalar;

            if (theta < 1e-9)
            {
                // Taylor series expansion of sin(θ/2) / θ = 1/2 - θ²/48 + O(θ⁴)
                axialScalar = 0.5 - theta * theta / 48.0;
            }
            else
            {
                axialScalar = Math.Sin(theta / 2.0) / theta;
            }

            return new Quaternion(
                cos,
                axialScalar * rvec[0],
                axialScalar * rvec[1],
                axialScalar * rvec[2]
            );
        }

        /// <summary>
        /// Converts this quaternion to a rotation vector (axis-angle representation).
        /// </summary>
        /// <returns>The rotation vector as a 3-element array [x, y, z].</returns>
        /// <remarks>
        /// This is also the logarithmic map (log operator) of SO(3).
        /// See equation (31) in "Integrating Generic Sensor Fusion Algorithms with
        /// Sound State Representation through Encapsulation of Manifolds"
        /// https://arxiv.org/pdf/1107.1119.pdf
        /// The vector's magnitude represents the rotation angle in radians,
        /// and its direction represents the rotation axis.
        /// </remarks>
        public double[] ToRotationVector()
        {
            double norm = Math.Sqrt(X * X + Y * Y + Z * Z);

            double coeff;
            if (norm < 1e-9)
            {
                coeff = 2.0 / W - 2.0 / 3.0 * norm * norm / (W * W * W);
            }
            else
            {
                if (W < 0.0)
                {
                    coeff = 2.0 * Math.Atan2(-norm, -W) / norm;
                }
                else
                {
                    coeff = 2.0 * Math.Atan2(norm, W) / norm;
                }
            }

            return new double[] { coeff * X, coeff * Y, coeff * Z };
        }

        /// <summary>
        /// Operator overload for quaternion addition.
        /// </summary>
        public static Quaternion operator +(Quaternion a, Quaternion b) => a.Plus(b);

        /// <summary>
        /// Operator overload for quaternion subtraction.
        /// </summary>
        public static Quaternion operator -(Quaternion a, Quaternion b) => a.Minus(b);

        /// <summary>
        /// Operator overload for quaternion multiplication.
        /// </summary>
        public static Quaternion operator *(Quaternion a, Quaternion b) => a.Times(b);

        /// <summary>
        /// Operator overload for scalar multiplication.
        /// </summary>
        public static Quaternion operator *(Quaternion q, double scalar) => q.Times(scalar);

        /// <summary>
        /// Operator overload for scalar multiplication (reversed order).
        /// </summary>
        public static Quaternion operator *(double scalar, Quaternion q) => q.Times(scalar);

        /// <summary>
        /// Operator overload for scalar division.
        /// </summary>
        public static Quaternion operator /(Quaternion q, double scalar) => q.Divide(scalar);

        /// <summary>
        /// Operator overload for equality comparison.
        /// </summary>
        public static bool operator ==(Quaternion a, Quaternion b)
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
        public static bool operator !=(Quaternion a, Quaternion b) => !(a == b);
    }
}
