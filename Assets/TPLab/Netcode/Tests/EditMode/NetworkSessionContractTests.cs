using System;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace TPLab.Netcode.Tests
{
    public class NetworkSessionContractTests
    {
        private static NetworkSessionIdentity Identity() => new NetworkSessionIdentity("s", "a", 1, 1, "scene");

        [TestCase(0, 1, 1, 1, "maxAdmissionPayloadBytes")]
        [TestCase(-1, 1, 1, 1, "maxAdmissionPayloadBytes")]
        [TestCase(100, 0, 1, 1, "preparationTimeoutSeconds")]
        [TestCase(100, double.NaN, 1, 1, "preparationTimeoutSeconds")]
        [TestCase(100, double.PositiveInfinity, 1, 1, "preparationTimeoutSeconds")]
        [TestCase(100, 1, -1, 1, "shutdownTimeoutSeconds")]
        [TestCase(100, 1, 1, 0, "restoreTimeoutSeconds")]
        public void LimitsRejectInvalidValues(int size, double prepare, double stop, double restore, string parameter)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkSessionOptions(size, prepare, stop, restore));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }

        [TestCase(0, "identity")]
        [TestCase(1, "participants")]
        [TestCase(2, "proof")]
        public void HostRequiresExplicitInputs(int missing, string parameter)
        {
            var error = Assert.Throws<ArgumentNullException>(() => NetworkSessionRequest.Host(
                missing == 0 ? null : Identity(), missing == 1 ? null : new[] { "host" },
                missing == 2 ? null : Array.Empty<byte>(), LoadSceneMode.Additive));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FrozenRosterRejectsEmptyBlankAndDuplicates(int kind)
        {
            string[] roster = kind == 0 ? Array.Empty<string>() : kind == 1 ? new[] { " " } : new[] { "host", "host" };
            var error = Assert.Throws<ArgumentException>(() => NetworkSessionRequest.Host(Identity(), roster, Array.Empty<byte>(), LoadSceneMode.Single));
            Assert.That(error.ParamName, Is.EqualTo("participants"));
        }

        [Test]
        public void HostModeRejectsUndefinedValue()
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => NetworkSessionRequest.Host(Identity(), new[] { "host" }, Array.Empty<byte>(), (LoadSceneMode)17));
            Assert.That(error.ParamName, Is.EqualTo("mode"));
        }

        [Test]
        public void RequestCopiesRosterAndOpaqueProof()
        {
            var roster = new[] { "host" }; var proof = new byte[] { 7 };
            var request = NetworkSessionRequest.Host(Identity(), roster, proof, LoadSceneMode.Additive);
            roster[0] = "changed"; proof[0] = 99;
            Assert.That(request.Participants, Is.EqualTo(new[] { "host" }));
            Assert.That(request.Proof.ToArray(), Is.EqualTo(new byte[] { 7 }));
        }
    }
}
