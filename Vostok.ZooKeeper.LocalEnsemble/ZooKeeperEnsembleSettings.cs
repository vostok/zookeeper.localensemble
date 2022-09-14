using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    /// <summary>
    /// Represents the configuration of a <see cref="ZooKeeperEnsemble"/>.
    /// </summary>
    [PublicAPI]
    public class ZooKeeperEnsembleSettings
    {
        /// <summary>
        /// If set to a non-null value, ensemble will be deployed to this directory.
        /// </summary>
        [CanBeNull]
        public string BaseDirectory { get; set; }

        /// <summary>
        /// If set to non-null value, <see cref="ZooKeeperInstance"/>s will be started on this ports.
        /// </summary>
        [CanBeNull]
        public IList<int> InstancesPorts { get; set; }

        /// <summary>
        /// Count of <see cref="ZooKeeperInstance"/>s in the ensemble.
        /// </summary>
        public int Size { get; set; } = 1;

        /// <summary>
        /// Id of the first <see cref="ZooKeeperInstance"/>.
        /// </summary>
        public int StartingId { get; set; } = 1;

        /// <summary>
        /// <para>If set to a non-null value, logs for each instance will be written here</para>
        /// <para>default location - in BaseDirectory</para>
        /// </summary>
        [CanBeNull]
        public string LogsDirectory { get; set; }
        
        /// <summary>
        /// <para>this name is used to build connection string</para>
        /// <para>default is 'localhost'</para>
        /// <para>does not affect listening options for ZK - it will listen all available intefaces</para>
        /// <para>known use cases: inside docker with ipv6 problems can specify '127.0.0.1' to prevent using ipv6 address (ZK may not listen it)</para>
        /// </summary>
        [CanBeNull]
        public string Hostname { get; set; }
    }
}