namespace RatioMaster.Tests
{
    using System;

    using NUnit.Framework;

    using RatioMaster_source;

    [TestFixture]
    public class VersionCheckerTests
    {
        [Test]
        public void GetServerVersionIdShouldReturnExactlyFourCharacters()
        {
            var versionChecker = new VersionChecker(string.Empty);
            var serverVersion = versionChecker.GetServerVersionId();
            Console.WriteLine(serverVersion);
            Assert.AreEqual(4, serverVersion.Length);
        }
    }
}
