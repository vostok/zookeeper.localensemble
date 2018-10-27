using NUnit.Framework;
using Vostok.Logging.Console;

namespace Vostok.ZooKeeper.LocalEnsemble.Tests
{
    [TestFixture]
    internal class TestLaunching
    {
        [Test]
        public void ClusterShouldBeAbleToStart()
        {
            using (ZooKeeperEnsemble.DeployNew(5, new ConsoleLog()))
            {
            }
        }
    }
}