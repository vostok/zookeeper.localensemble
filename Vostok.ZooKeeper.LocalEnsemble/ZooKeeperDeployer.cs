using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Vostok.ZooKeeper.LocalEnsemble
{
	internal static class ZooKeeperDeployer
	{
		public static void DeployInstances(List<ZooKeeperInstance> instances)
		{
			var configs = GenerateConfigs(instances);
			for (var i = 0; i < instances.Count; i++)
				DeployInstance(instances[i], configs[i]);
		}

		public static void CleanupInstances(List<ZooKeeperInstance> instances)
		{
			foreach (var instance in instances)
			{
				for (var i = 0; i < 3; i++)
					try
					{
						Directory.Delete(instance.BaseDirectory, true);
						break;
					}
					catch (Exception)
					{
						Thread.Sleep(500);
					}
			}
		}

		private static void DeployInstance(ZooKeeperInstance instance, string config)
		{
			CreateDirectories(instance);
			DeployFiles(instance, config);
		}

		private static string[] GenerateConfigs(List<ZooKeeperInstance> instances)
		{
			var serversList = String.Join(Environment.NewLine, instances.Select(instance => $"server.{instance.Id}=localhost:{instance.PeerPort}:{instance.ElectionPort}"));
			var configs = new string[instances.Count];
			for (var i = 0; i < configs.Length; i++)
			{
				var configBuilder = new StringBuilder();
				configBuilder.AppendLine($"tickTime={2000}{Environment.NewLine}");
				configBuilder.AppendLine($"initLimit={10}{Environment.NewLine}");
				configBuilder.AppendLine($"syncLimit={10}{Environment.NewLine}");
				configBuilder.AppendLine($"dataDir=data{Environment.NewLine}");
				configBuilder.AppendLine($"clientPort={instances[i].ClientPort}{Environment.NewLine}");
				configBuilder.AppendLine(serversList);
				configBuilder.AppendLine($"maxClientCnxns={0}{Environment.NewLine}");
				configs[i] = configBuilder.ToString();
			}
			return configs;
		}

		private static void CreateDirectories(ZooKeeperInstance instance)
		{
			if (Directory.Exists(instance.BaseDirectory))
				Directory.Delete(instance.BaseDirectory, true);
			Directory.CreateDirectory(instance.BaseDirectory);
			Directory.CreateDirectory(instance.BinDirectory);
			Directory.CreateDirectory(instance.DataDirectory);
			Directory.CreateDirectory(instance.LibDirectory);
			Directory.CreateDirectory(instance.ConfDirectory);
		}

		private static void DeployFiles(ZooKeeperInstance instance, string config)
		{
			// (iloktionov): Libraries.
			File.WriteAllBytes(Path.Combine(instance.LibDirectory, "jline_0_9_94.jar"), Properties.Resources.jline_0_9_94);
			File.WriteAllBytes(Path.Combine(instance.LibDirectory, "log4j_1_2_15.jar"), Properties.Resources.log4j_1_2_15);
			File.WriteAllBytes(Path.Combine(instance.LibDirectory, "netty_3_2_2_Final.jar"), Properties.Resources.netty_3_2_2_Final);
			File.WriteAllBytes(Path.Combine(instance.LibDirectory, "slf4j_api_1_6_1.jar"), Properties.Resources.slf4j_api_1_6_1);
			File.WriteAllBytes(Path.Combine(instance.LibDirectory, "slf4j_log4j12_1_6_1.jar"), Properties.Resources.slf4j_log4j12_1_6_1);
			// (iloktionov): Control scripts. 
			File.WriteAllText(Path.Combine(instance.BinDirectory, "zkEnv.cmd"), Properties.Resources.zkEnv);
			File.WriteAllText(Path.Combine(instance.BinDirectory, "zkServer.cmd"), Properties.Resources.zkServer);
			// (iloktionov): Actual ZK lib.
			File.WriteAllBytes(Path.Combine(instance.BaseDirectory, "zookeeper_3_4_5.jar"), Properties.Resources.zookeeper_3_4_5);
			// (iloktionov): Configs.
			File.WriteAllText(Path.Combine(instance.ConfDirectory, "zoo.cfg"), config);
			File.WriteAllText(Path.Combine(instance.ConfDirectory, "log4j.properties"), CreateLog4jConfig(instance));
			// (iloktionov): Self id.
			File.WriteAllText(Path.Combine(instance.DataDirectory, "myid"), instance.Id.ToString());
		}

		private static string CreateLog4jConfig(ZooKeeperInstance instance)
		{
			var builder = new StringBuilder();
			builder.AppendLine("log4j.rootLogger=DEBUG, ROLLINGFILE");
			builder.AppendLine("log4j.appender.ROLLINGFILE=org.apache.log4j.RollingFileAppender");
			builder.AppendLine("log4j.appender.ROLLINGFILE.File=" + $"../ZK-{instance.Id}.log");
			builder.AppendLine("log4j.appender.ROLLINGFILE.Threshold=DEBUG");
			builder.AppendLine("log4j.appender.ROLLINGFILE.layout=org.apache.log4j.PatternLayout");
			builder.AppendLine("log4j.appender.ROLLINGFILE.layout.ConversionPattern=[myid:%X{myid}] - %d %-5p [%t:%C{1}@%L] - %m%n");
			return builder.ToString();
		}
	}
}