using NUnit.Framework;

namespace TPLab.Core.Tests
{
    public sealed class SmokeIgnoreTests
    {
        [Test, Ignore("smoke")]
        public void Smoke()
        {
            Assert.Pass();
        }
    }
}
