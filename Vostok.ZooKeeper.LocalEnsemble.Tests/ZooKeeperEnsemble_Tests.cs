using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Commons.Helpers.Network;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.ZooKeeper.LocalEnsemble.Tests
{
    [TestFixture]
    internal class ZooKeeperEnsemble_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        [Test]
        public void Can_use_Ip_In_Hostname()
        {
            var hostname = IPAddress.Loopback.ToString();
            using (var ensemble = ZooKeeperEnsemble.DeployNew(
                new ZooKeeperEnsembleSettings
                {
                    Size = 2,
                    Hostname = hostname
                },
                log))
            {
                ensemble.IsRunning.Should().BeTrue();
                Console.WriteLine(ensemble.ConnectionString);
                ensemble.ConnectionString.Contains(hostname).Should().Be(true);
                (ensemble.ConnectionString.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) < 0).Should().Be(true);
            }
        }


        [Test]
        public void DeployNew_should_run_place_logs_in_directory()
        {
            var logsDirectory = Path.GetFullPath("zk-logs");
            if (Directory.Exists(logsDirectory))
                Directory.Delete(logsDirectory, true);

            var instances = 2;
            var settings = new ZooKeeperEnsembleSettings {Size = instances, LogsDirectory = logsDirectory};
            using (var ensemble = ZooKeeperEnsemble.DeployNew(settings, log))
            {
                ensemble.IsRunning.Should().BeTrue();
            }

            var logs = Directory.GetFiles(settings.LogsDirectory).ToArray();
            logs.Length.Should().Be(instances);
            for (var i = 1; i < 1 + instances; i++)
            {
                File.Exists(Path.Combine(settings.LogsDirectory, $"ZK-{i}.log")).Should().Be(true);
            }
        }

        [TestCase(1)]
        [TestCase(5)]
        public void DeployNew_should_run_instances(int instances)
        {
            using (var ensemble = ZooKeeperEnsemble.DeployNew(instances, log))
            {
                ensemble.IsRunning.Should().BeTrue();
            }
        }

        [Test]
        public void DeployNew_should_use_from_index()
        {
            using (var ensemble1 = ZooKeeperEnsemble.DeployNew(1, 3, log))
            using (var ensemble2 = ZooKeeperEnsemble.DeployNew(10, 3, log))
            {
                ensemble1.IsRunning.Should().BeTrue();
                ensemble2.IsRunning.Should().BeTrue();
            }
        }

        [TestCase(3)]
        public void DeployNew_should_not_run_instances_if_specified(int instances)
        {
            using (var ensemble = ZooKeeperEnsemble.DeployNew(instances, log, false))
            {
                ensemble.IsRunning.Should().BeFalse();
            }
        }

        [TestCase(3)]
        public void DeployNew_should_run_multiple_times(int instances)
        {
            using (ZooKeeperEnsemble.DeployNew(instances, log))
            {
            }

            using (ZooKeeperEnsemble.DeployNew(instances, log))
            {
            }
        }

        [Test]
        public void DeployNew_should_run_instances_on_specified_ports()
        {
            var port1 = FreeTcpPortFinder.GetFreePort();
            var port2 = FreeTcpPortFinder.GetFreePort();

            using (var ensemble = ZooKeeperEnsemble.DeployNew(
                new ZooKeeperEnsembleSettings
                {
                    Size = 2,
                    InstancesPorts = new List<int> {port1, port2}
                },
                log))
            {
                ensemble.IsRunning.Should().BeTrue();
                ensemble.Instances.Count.Should().Be(2);
                ensemble.Instances[0].ClientPort.Should().Be(port1);
                ensemble.Instances[1].ClientPort.Should().Be(port2);
            }
        }

        [Test]
        public void DeployNew_should_fail_to_start_if_port_is_busy()
        {
            var port = FreeTcpPortFinder.GetFreePort();
            var listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
                Action deployment = () => ZooKeeperEnsemble.DeployNew(
                    new ZooKeeperEnsembleSettings
                    {
                        InstancesPorts = new List<int> {port}
                    },
                    log);

                deployment.Should().Throw<Exception>();
            }
            finally
            {
                listener.Stop();
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Instances_should_be_stoppable_and_startable(int index)
        {
            using (var ensemble = ZooKeeperEnsemble.DeployNew(3, log))
            {
                ensemble.Instances[index].IsRunning.Should().BeTrue("Before stop.");
                ensemble.Instances[index].Stop();
                ensemble.Instances[index].IsRunning.Should().BeFalse("After stop.");
                ensemble.Instances[index].Start();
                ensemble.Instances[index].IsRunning.Should().BeTrue("After restart.");
            }
        }
    }
}