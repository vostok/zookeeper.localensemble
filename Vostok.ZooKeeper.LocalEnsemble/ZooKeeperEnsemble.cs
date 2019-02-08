using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    [PublicAPI]
    public class ZooKeeperEnsemble : IDisposable
    {
        private readonly ILog log;

        private volatile bool isRunning;
        private volatile bool isDisposed;

        public ZooKeeperEnsemble(int size, ILog log)
        {
            this.log = log.ForContext("ZKEnsemble");
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            Instances = CreateInstances(size, this.log);
            LogInstances();
        }

        public static ZooKeeperEnsemble DeployNew(int size, ILog log)
        {
            var ensemble = new ZooKeeperEnsemble(size, log);
            ensemble.Deploy();
            return ensemble;
        }

        public bool IsDisposed => isDisposed;

        public bool IsRunning => isRunning;

        public List<ZooKeeperInstance> Instances { get; }

        public string ConnectionString
        {
            get { return string.Join(",", Instances.Select(instance => $"localhost:{instance.ClientPort}")); }
        }

        public void Deploy()
        {
            try
            {
                ZooKeeperDeployer.DeployInstances(Instances);
                Start();
            }
            catch (Exception error)
            {
                LogErrorInStarting(error);
                Dispose();
                throw;
            }
        }

        public void Start()
        {
            if (!isRunning)
            {
                LogStarting();
                foreach (var instance in Instances)
                    instance.Start();
                WaitAndCheckInstancesAreRunning(Instances);
                LogStarted();
                isRunning = true;
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                LogStopping();
                foreach (var instance in Instances)
                    instance.Stop();
                LogStopped();
                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                Stop();
                LogCleaning();
                ZooKeeperDeployer.CleanupInstances(Instances);
                LogCleaned();
                isDisposed = true;
            }
        }

        private static List<ZooKeeperInstance> CreateInstances(int size, ILog log)
        {
            var instances = new List<ZooKeeperInstance>(size);
            for (var i = 1; i <= size; i++)
            {
                var clientPort = FreeTcpPortFinder.GetFreePort();
                var peerPort = FreeTcpPortFinder.GetFreePort();
                var electionPort = FreeTcpPortFinder.GetFreePort();
                instances.Add(new ZooKeeperInstance(i, new DirectoryInfo("ZK-" + i).FullName, clientPort, peerPort, electionPort, log));
            }

            return instances;
        }

        public static void WaitAndCheckInstancesAreRunning(List<ZooKeeperInstance> instances)
        {
            var timeout = TimeSpan.FromSeconds(5);
            var watch = Stopwatch.StartNew();
            var idleInstances = 0;
            while (watch.Elapsed < timeout)
            {
                idleInstances = instances.Count(instance => !instance.IsRunning());
                if (idleInstances == 0)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    return;
                }

                Thread.Sleep(100);
            }

            throw new Exception($"{idleInstances} of {instances.Count} instances have not started.");
        }

        #region Logging

        private void LogInstances()
        {
            log.Info("Created instances: \n\t" + string.Join("\n\t", Instances.Select(i => i.ToString())));
        }

        private void LogStarting()
        {
            log.Info("Starting instances..");
        }

        private void LogStarted()
        {
            log.Info("Started successfully!");
        }

        private void LogErrorInStarting(Exception error)
        {
            log.Error("Error in starting. Will try to stop. Exception: {0}", error);
        }

        private void LogStopping()
        {
            log.Info("Stopping instances..");
        }

        private void LogStopped()
        {
            log.Info("Stopped successfully!");
        }

        private void LogCleaning()
        {
            log.Info("Cleaning directories..");
        }

        private void LogCleaned()
        {
            log.Info("Cleaned directories successfully!");
        }

        #endregion
    }
}