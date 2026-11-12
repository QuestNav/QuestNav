using System;
using NUnit.Framework;
using QuestNav.QuestNav.Geometry;
using Wpi.Proto;

namespace QuestNav.Tests
{
    public class Rotation3dTests
    {
        private const double Tolerance = 1e-12;

        private static Rotation3d FromProtobuf(double w, double x, double y, double z)
        {
            return new Rotation3d(
                new ProtobufRotation3d
                {
                    Q = new ProtobufQuaternion
                    {
                        W = w,
                        X = x,
                        Y = y,
                        Z = z,
                    },
                }
            );
        }

        [Test]
        public void ProtobufConstructor_NormalizesQuaternion()
        {
            var q = FromProtobuf(2, 0, 0, 2).Quaternion;

            Assert.AreEqual(1.0, q.Norm(), Tolerance);
            Assert.AreEqual(Math.Sqrt(0.5), q.W, Tolerance);
            Assert.AreEqual(Math.Sqrt(0.5), q.Z, Tolerance);
        }

        [Test]
        public void ProtobufConstructor_ZeroQuaternionBecomesIdentity()
        {
            var q = FromProtobuf(0, 0, 0, 0).Quaternion;

            Assert.AreEqual(1.0, q.W, Tolerance);
            Assert.AreEqual(0.0, q.X, Tolerance);
            Assert.AreEqual(0.0, q.Y, Tolerance);
            Assert.AreEqual(0.0, q.Z, Tolerance);
        }

        [Test]
        public void ProtobufConstructor_MatchesQuaternionConstructor()
        {
            var fromProto = FromProtobuf(0.9, 0.1, -0.3, 0.2);
            var fromQuaternion = new Rotation3d(new Quaternion(0.9, 0.1, -0.3, 0.2));

            Assert.AreEqual(fromQuaternion, fromProto);
        }
    }
}
