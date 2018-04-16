using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Amazon.XRay.Recorder.Core.Internal.Emitters
{
    public class HostEndpoint
    {
        private HostEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; private set; }
        public int Port { get; private set; }

        public IPEndPoint GetIPEndPoint()
        {
            var ipEntries = Dns.GetHostAddresses(Host);
            return new IPEndPoint(ipEntries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork), Port);
        }
        
        public static bool TryParse(string entry, out HostEndpoint hostEndpoint)
        {
            var entries = entry.Split(':');
            if (entries.Length != 2)
            {
                hostEndpoint = null;
                return false;
            }
            if (!int.TryParse(entries[1], out var port))
            {
                hostEndpoint = null;
                return false;
            }
            hostEndpoint = new HostEndpoint(entries[0], port);
            return true;
        }
    }
}