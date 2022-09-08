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
        /// If set to a non-null value, logs for each instance will be written here
        /// default location - in BaseDirectory
        /// </summary>
        [CanBeNull]
        public string LogsDirectory { get; set; }
    }
}