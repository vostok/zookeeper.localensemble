using JetBrains.Annotations;

namespace Vostok.ZooKeeper.LocalEnsemble
{
    [PublicAPI]
    public class ZooKeeperInstanceSettings
    {
        public ZooKeeperInstanceSettings(int id, string baseDirectory, int clientPort, int peerPort, int electionPort, string hostname)
        {
            Id = id;
            BaseDirectory = baseDirectory;
            ClientPort = clientPort;
            PeerPort = peerPort;
            ElectionPort = electionPort;
            Hostname = hostname;
        }

        public int Id { get; }
        public string BaseDirectory { get; }
        public int ClientPort { get; }
        public int PeerPort { get; }
        public int ElectionPort { get; }
        public string Hostname { get; }
    }
}