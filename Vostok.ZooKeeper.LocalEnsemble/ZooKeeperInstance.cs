using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    public class ZooKeeperInstance
    {
        private const string serverScriptName = "zkServer.cmd";

        private Process process;

        public ZooKeeperInstance(int id, string baseDirectory, int clientPort, int peerPort, int electionPort, ILog log)
        {
            Id = id;
            BaseDirectory = baseDirectory;
            ClientPort = clientPort;
            PeerPort = peerPort;
            ElectionPort = electionPort;
        }

        public int Id { get; }

        public int ClientPort { get; }

        public int PeerPort { get; }

        public int ElectionPort { get; }

        public string BaseDirectory { get; }

        public string LibDirectory => Path.Combine(BaseDirectory, "lib");

        public string BinDirectory => Path.Combine(BaseDirectory, "bin");

        public string ConfDirectory => Path.Combine(BaseDirectory, "conf");

        public string DataDirectory => Path.Combine(BinDirectory, "data");

        public string LogFileName => Path.Combine(BaseDirectory, "ZK-" + Id + ".log");

        public bool IsRunning()
        {
            return process != null && !process.HasExited;
        }

        public void Start()
        {
            var processStartInfo = new ProcessStartInfo(Path.Combine(BinDirectory, serverScriptName))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                WorkingDirectory = BinDirectory
            };
            process = new Process
            {
                StartInfo = processStartInfo
            };
            if (!process.Start())
                throw new Exception($"Failed to start process of participant '{Id}'.");

            WaitForInstanceToStart();
            WaitTillJavaProcessSpawns(process, TimeSpan.FromSeconds(1));
        }

        public void Stop()
        {
            if (process == null)
                return;
            if (!process.HasExited)
            {
                foreach (var childProcess in GetChildJavaProcesses(process.Id))
                {
                    if (!childProcess.HasExited)
                        try
                        {
                            childProcess.Kill();
                            childProcess.WaitForExit();
                        }
                        catch
                        {
                        }
                }

                if (!process.HasExited)
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch
                    {
                    }
            }

            process = null;
        }

        public override string ToString()
        {
            return $"localhost:{ClientPort}:{PeerPort}:{ElectionPort} (id {Id}) at '{BaseDirectory}'";
        }

        private static void WaitForInstanceToStart()
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
        }

        private static void WaitTillJavaProcessSpawns(Process parentProcess, TimeSpan timeout)
        {
            var budget = TimeBudget.StartNew(timeout);

            while (!budget.HasExpired)
            {
                if (GetChildJavaProcesses(parentProcess.Id).Any())
                    return;

                Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }
        }

        private static IEnumerable<Process> GetChildJavaProcesses(int parentId)
        {
            foreach (var process in Process.GetProcesses().Where(x => x.ProcessName.Contains("java")))
            {
                yield return process;
            }
        }
    }
}