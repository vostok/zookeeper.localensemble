using System;
using System.IO;
using JetBrains.Annotations;
using Vostok.Commons.Local;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.LocalEnsemble.Helpers;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    /// <summary>
    /// Represents a locally run instance based of ZooKeeper service.
    /// </summary>
    [PublicAPI]
    public class ZooKeeperInstance
    {
        private readonly string hostname;
        private readonly ILog log;
        private readonly ZooKeeperHealthChecker healthChecker;
        private readonly ShellRunner runner;

        public ZooKeeperInstance(ZooKeeperInstanceSettings settings, ILog log)
        {
            Id = settings.Id;
            this.log = log = log.ForContext($"Instance-{Id}");

            BaseDirectory = settings.BaseDirectory;
            ClientPort = settings.ClientPort;
            PeerPort = settings.PeerPort;
            ElectionPort = settings.ElectionPort;
            hostname = settings.Hostname;
            healthChecker = new ZooKeeperHealthChecker(log, hostname, ClientPort);
            runner = new ShellRunner(new ShellRunnerSettings("java")
                {
                    Arguments = BuildZooKeeperArguments(),
                    WorkingDirectory = BinDirectory
                },
                log);
        }

        [Obsolete]
        public ZooKeeperInstance(int id, string baseDirectory, int clientPort, int peerPort, int electionPort, ILog log)
            : this(new ZooKeeperInstanceSettings(id, baseDirectory, clientPort, peerPort, electionPort, "localhost"), log)
        {
        }

        /// <summary>
        /// Returns numeric instance id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Returns instance client port.
        /// </summary>
        public int ClientPort { get; }

        /// <summary>
        /// Returns instance peer port.
        /// </summary>
        public int PeerPort { get; }

        /// <summary>
        /// Returns instance election port.
        /// </summary>
        public int ElectionPort { get; }

        /// <summary>
        /// Returns instance base directory.
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// Returns instance lib directory.
        /// </summary>
        public string LibDirectory => Path.Combine(BaseDirectory, "lib");

        /// <summary>
        /// Returns instance bin directory.
        /// </summary>
        public string BinDirectory => Path.Combine(BaseDirectory, "bin");

        /// <summary>
        /// Returns instance configuration directory.
        /// </summary>
        public string ConfDirectory => Path.Combine(BaseDirectory, "conf");

        /// <summary>
        /// Returns instance data directory.
        /// </summary>
        public string DataDirectory => Path.Combine(BinDirectory, "data");

        /// <summary>
        /// Returns whether this instance is currently running.
        /// </summary>
        public bool IsRunning => runner.IsRunning;

        /// <summary>
        /// <para>Starts instance.</para>
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;

            runner.Start();

            if (!healthChecker.WaitStarted(20.Seconds()))
                throw new Exception($"instance '{Id}' has not warmed up in 20 seconds..");
        }

        public void Stop() => runner.Stop();

        public override string ToString()
            => $"{hostname}:{ClientPort}:{PeerPort}:{ElectionPort} (id {Id}) at '{BaseDirectory}'";

        private string BuildZooKeeperArguments()
        {
            var classPaths = new[]
            {
                Path.Combine(BaseDirectory, "*"),
                Path.Combine(LibDirectory, "*"),
                ConfDirectory
            };
            var joinedClassPaths = string.Join(OsHelper.PathDelimiter, classPaths);

            var result = $"\"-Dzookeeper.log.dir={BaseDirectory}\" \"-Dzookeeper.root.logger=DEBUG,ROLLINGFILE\" -cp \"{joinedClassPaths}\" org.apache.zookeeper.server.quorum.QuorumPeerMain \"{Path.Combine(ConfDirectory, "zoo.cfg")}\"";

            return result;
        }
    }
}