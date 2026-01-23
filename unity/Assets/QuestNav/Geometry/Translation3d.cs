using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Wpi.Proto;

namespace QuestNav.QuestNav.Geometry
{
    /// <summary>
    /// Represents a translation in 3D space. This object can be used to represent a point or a vector.
    /// </summary>
    /// <remarks>
    /// This assumes that you are using conventional mathematical axes. When the robot is at the
    /// origin facing in the positive X direction, forward is positive X, left is positive Y, and up is
    /// positive Z.
    /// </remarks>
    public class Translation3d : IEquatable<Translation3d>
    {
        /// <summary>
        /// A preallocated Translation3d representing the origin (0, 0, 0).
        /// </summary>
        /// <remarks>
        /// This exists to avoid allocations for common translations.
        /// </remarks>
        public static readonly Translation3d Zero = new Translation3d();

        /// <summary>
        /// Gets the X component of the translation.
        /// </summary>
        [JsonProperty("x")]
        public double X { get; }

        /// <summary>
        /// Gets the Y component of the translation.
        /// </summary>
        [JsonProperty("y")]
        public double Y { get; }

        /// <summary>
        /// Gets the Z component of the translation.
        /// </summary>
        [JsonProperty("z")]
        public double Z { get; }

        /// <summary>
        /// Constructs a Translation3d with X, Y, and Z components equal to zero.
        /// </summary>
        public Translation3d()
            : this(0.0, 0.0, 0.0) { }

        /// <summary>
        /// Constructs a Translation3d with the X, Y, and Z components equal to the provided values.
        /// </summary>
        /// <param name="x">The x component of the translation.</param>
        /// <param name="y">The y component of the translation.</param>
        /// <param name="z">The z component of the translation.</param>
        [JsonConstructor]
        public Translation3d(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Constructs a Translation3d with the provided distance and angle.
        /// </summary>
        /// <param name="distance">The distance from the origin to the end of the translation.</param>
        /// <param name="angle">The angle (rotation) between the x-axis and the translation vector.</param>
        /// <remarks>
        /// This is essentially converting from spherical coordinates to Cartesian coordinates.
        /// </remarks>
        public Translation3d(double distance, Rotation3d angle)
        {
            if (angle == null)
                throw new ArgumentNullException(nameof(angle));

            var rectangular = new Translation3d(distance, 0.0, 0.0).RotateBy(angle);
            X = rectangular.X;
            Y = rectangular.Y;
            Z = rectangular.Z;
        }

        /// <summary>
        /// Constructs a Translation3d from a 3D vector. The values are assumed to be in meters.
        /// </summary>
        /// <param name="vector">The translation vector as a 3-element array [x, y, z].</param>
        public Translation3d(double[] vector)
        {
            if (vector == null)
                throw new ArgumentNullException(nameof(vector));
            if (vector.Length != 3)
                throw new ArgumentException("Vector must have exactly 3 elements", nameof(vector));

            X = vector[0];
            Y = vector[1];
            Z = vector[2];
        }

        /// <summary>
        /// Creates a new Translation3d from its protobuf counterpart
        /// </summary>
        /// <param name="protobuf">The protobuf to convert</param>
        public Translation3d(ProtobufTranslation3d protobuf)
        {
            X = protobuf.X;
            Y = protobuf.Y;
            Z = protobuf.Z;
        }

        /// <summary>
        /// Copies the data to a protobuf-sterilized version to be sent over NT
        /// </summary>
        /// <returns>The sterilized protobuf</returns>
        public ProtobufTranslation3d ToProtobuf()
        {
            var proto = new ProtobufTranslation3d
            {
                X = X,
                Y = Y,
                Z = Z,
            };
            return proto;
        }

        /// <summary>
        /// Calculates the distance between two translations in 3D space.
        /// </summary>
        /// <param name="other">The translation to compute the distance to.</param>
        /// <returns>The distance between the two translations.</returns>
        /// <remarks>
        /// The distance between translations is defined as √((x₂−x₁)²+(y₂−y₁)²+(z₂−z₁)²).
        /// </remarks>
        public double GetDistance(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            double dx = other.X - X;
            double dy = other.Y - Y;
            double dz = other.Z - Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        /// <summary>
        /// Calculates the squared distance between two translations in 3D space.
        /// </summary>
        /// <param name="other">The translation to compute the squared distance to.</param>
        /// <returns>The squared distance between the two translations.</returns>
        /// <remarks>
        /// This is equivalent to squaring the result of <see cref="GetDistance"/>, but avoids computing a square root.
        /// The squared distance between translations is defined as (x₂−x₁)²+(y₂−y₁)²+(z₂−z₁)².
        /// </remarks>
        public double GetSquaredDistance(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            double dx = other.X - X;
            double dy = other.Y - Y;
            double dz = other.Z - Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Converts this translation to a 3D vector representation.
        /// </summary>
        /// <returns>A 3-element array [x, y, z] representing this translation as a vector.</returns>
        public double[] ToVector()
        {
            return new double[] { X, Y, Z };
        }

        /// <summary>
        /// Returns the norm, or distance from the origin to the translation.
        /// </summary>
        /// <returns>The norm of the translation.</returns>
        public double Norm => Math.Sqrt(X * X + Y * Y + Z * Z);

        /// <summary>
        /// Returns the squared norm, or squared distance from the origin to the translation.
        /// </summary>
        /// <returns>The squared norm of the translation.</returns>
        /// <remarks>
        /// This is equivalent to squaring the result of <see cref="Norm"/>, but avoids computing a square root.
        /// </remarks>
        public double SquaredNorm => X * X + Y * Y + Z * Z;

        /// <summary>
        /// Applies a rotation to the translation in 3D space.
        /// </summary>
        /// <param name="other">The rotation to rotate the translation by.</param>
        /// <returns>The new rotated translation.</returns>
        /// <remarks>
        /// For example, rotating a Translation3d of &lt;2, 0, 0&gt; by 90 degrees around the Z axis
        /// will return a Translation3d of &lt;0, 2, 0&gt;.
        /// </remarks>
        public Translation3d RotateBy(Rotation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var p = new Quaternion(0.0, X, Y, Z);
            var qprime = other.Quaternion * p * other.Quaternion.Inverse();
            return new Translation3d(qprime.X, qprime.Y, qprime.Z);
        }

        /// <summary>
        /// Rotates this translation around another translation in 3D space.
        /// </summary>
        /// <param name="other">The other translation to rotate around.</param>
        /// <param name="rotation">The rotation to rotate the translation by.</param>
        /// <returns>The new rotated translation.</returns>
        public Translation3d RotateAround(Translation3d other, Rotation3d rotation)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (rotation == null)
                throw new ArgumentNullException(nameof(rotation));

            return Minus(other).RotateBy(rotation).Plus(other);
        }

        /// <summary>
        /// Computes the dot product between this translation and another translation in 3D space.
        /// </summary>
        /// <param name="other">The translation to compute the dot product with.</param>
        /// <returns>The dot product between the two translations.</returns>
        /// <remarks>
        /// The dot product between two translations is defined as x₁x₂+y₁y₂+z₁z₂.
        /// </remarks>
        public double Dot(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return X * other.X + Y * other.Y + Z * other.Z;
        }

        /// <summary>
        /// Computes the cross product between this translation and another translation in 3D space.
        /// </summary>
        /// <param name="other">The translation to compute the cross product with.</param>
        /// <returns>The cross product between the two translations as a 3-element array.</returns>
        /// <remarks>
        /// The resulting translation will be perpendicular to both translations.
        /// The 3D cross product between two translations is defined as &lt;y₁z₂-y₂z₁, z₁x₂-z₂x₁, x₁y₂-x₂y₁&gt;.
        /// </remarks>
        public double[] Cross(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new double[]
            {
                Y * other.Z - other.Y * Z,
                Z * other.X - other.Z * X,
                X * other.Y - other.X * Y,
            };
        }

        /// <summary>
        /// Returns the sum of two translations in 3D space.
        /// </summary>
        /// <param name="other">The translation to add.</param>
        /// <returns>The sum of the translations.</returns>
        /// <remarks>
        /// For example, Translation3d(1.0, 2.5, 3.5) + Translation3d(2.0, 5.5, 7.5) = Translation3d(3.0, 8.0, 11.0).
        /// </remarks>
        public Translation3d Plus(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new Translation3d(X + other.X, Y + other.Y, Z + other.Z);
        }

        /// <summary>
        /// Returns the difference between two translations.
        /// </summary>
        /// <param name="other">The translation to subtract.</param>
        /// <returns>The difference between the two translations.</returns>
        /// <remarks>
        /// For example, Translation3d(5.0, 4.0, 3.0) - Translation3d(1.0, 2.0, 3.0) = Translation3d(4.0, 2.0, 0.0).
        /// </remarks>
        public Translation3d Minus(Translation3d other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return new Translation3d(X - other.X, Y - other.Y, Z - other.Z);
        }

        /// <summary>
        /// Returns the inverse of the current translation.
        /// </summary>
        /// <returns>The inverse of the current translation.</returns>
        /// <remarks>
        /// This is equivalent to negating all components of the translation.
        /// </remarks>
        public Translation3d UnaryMinus()
        {
            return new Translation3d(-X, -Y, -Z);
        }

        /// <summary>
        /// Returns the translation multiplied by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar to multiply by.</param>
        /// <returns>The scaled translation.</returns>
        /// <remarks>
        /// For example, Translation3d(2.0, 2.5, 4.5) * 2 = Translation3d(4.0, 5.0, 9.0).
        /// </remarks>
        public Translation3d Times(double scalar)
        {
            return new Translation3d(X * scalar, Y * scalar, Z * scalar);
        }

        /// <summary>
        /// Returns the translation divided by a scalar.
        /// </summary>
        /// <param name="scalar">The scalar to divide by.</param>
        /// <returns>The scaled translation.</returns>
        /// <remarks>
        /// For example, Translation3d(2.0, 2.5, 4.5) / 2 = Translation3d(1.0, 1.25, 2.25).
        /// </remarks>
        public Translation3d Div(double scalar)
        {
            return new Translation3d(X / scalar, Y / scalar, Z / scalar);
        }

        /// <summary>
        /// Returns the nearest Translation3d from a collection of translations.
        /// </summary>
        /// <param name="translations">The collection of translations to search.</param>
        /// <returns>The nearest Translation3d from the collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown if translations is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
        public Translation3d Nearest(IEnumerable<Translation3d> translations)
        {
            if (translations == null)
                throw new ArgumentNullException(nameof(translations));

            return translations.OrderBy(t => GetDistance(t)).First();
        }

        /// <summary>
        /// Returns a string representation of this translation.
        /// </summary>
        public override string ToString()
        {
            return $"Translation3d(X: {X:F2}, Y: {Y:F2}, Z: {Z:F2})";
        }

        /// <summary>
        /// Checks equality between this Translation3d and another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is Translation3d other && Equals(other);
        }

        /// <summary>
        /// Checks equality between this Translation3d and another Translation3d.
        /// </summary>
        /// <param name="other">The other Translation3d.</param>
        /// <returns>True if the translations are equal within a tolerance of 1e-9, false otherwise.</returns>
        public bool Equals(Translation3d other)
        {
            if (other == null)
                return false;
            return Math.Abs(other.X - X) < 1e-9
                && Math.Abs(other.Y - Y) < 1e-9
                && Math.Abs(other.Z - Z) < 1e-9;
        }

        /// <summary>
        /// Returns a hash code for this translation.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        /// <summary>
        /// Operator overload for translation addition.
        /// </summary>
        public static Translation3d operator +(Translation3d a, Translation3d b) => a.Plus(b);

        /// <summary>
        /// Operator overload for translation subtraction.
        /// </summary>
        public static Translation3d operator -(Translation3d a, Translation3d b) => a.Minus(b);

        /// <summary>
        /// Operator overload for unary negation (inverse).
        /// </summary>
        public static Translation3d operator -(Translation3d a) => a.UnaryMinus();

        /// <summary>
        /// Operator overload for scalar multiplication.
        /// </summary>
        public static Translation3d operator *(Translation3d translation, double scalar) =>
            translation.Times(scalar);

        /// <summary>
        /// Operator overload for scalar multiplication (reversed order).
        /// </summary>
        public static Translation3d operator *(double scalar, Translation3d translation) =>
            translation.Times(scalar);

        /// <summary>
        /// Operator overload for scalar division.
        /// </summary>
        public static Translation3d operator /(Translation3d translation, double scalar) =>
            translation.Div(scalar);

        /// <summary>
        /// Operator overload for equality comparison.
        /// </summary>
        public static bool operator ==(Translation3d a, Translation3d b)
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
        public static bool operator !=(Translation3d a, Translation3d b) => !(a == b);
    }
}
