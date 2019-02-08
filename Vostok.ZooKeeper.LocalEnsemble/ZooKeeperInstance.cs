using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    public class ZooKeeperInstance
    {
        private const string serverScriptName = "zkServer.cmd";

        private readonly WindowsProcessKillJob processKillJob;

        private Process process;

        public ZooKeeperInstance(int id, string baseDirectory, int clientPort, int peerPort, int electionPort, ILog log)
        {
            Id = id;
            BaseDirectory = baseDirectory;
            ClientPort = clientPort;
            PeerPort = peerPort;
            ElectionPort = electionPort;
            processKillJob = new WindowsProcessKillJob(log);
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

            InstancesHelper.WaitAndCheckInstancesAreRunning(new List<ZooKeeperInstance> {this});
            WaitTillJavaProcessSpawns(process, TimeSpan.FromSeconds(1));

            foreach (var childProcess in GetChildJavaProcesses(process.Id))
                processKillJob.AddProcess(childProcess);

            processKillJob.AddProcess(process);
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
            using (var searcher = new ManagementObjectSearcher("select ProcessId from Win32_Process where ParentProcessId=" + parentId))
            {
                return searcher
                    .Get()
                    .Cast<ManagementBaseObject>()
                    .Select(m => Convert.ToInt32(m["ProcessId"]))
                    .Select(Process.GetProcessById)
                    .Where(p => p.ProcessName.Contains("java"));
            }
        }
    }
}