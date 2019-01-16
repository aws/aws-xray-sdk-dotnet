using System.Net;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    public class EndPoint
    {
        private HostEndPoint _h;
        private IPEndPoint _i;
        private bool _isHost;

        public static EndPoint of(HostEndPoint hostEndPoint)
        {
            return new EndPoint {_isHost = true, _h = hostEndPoint};
        }

        public static EndPoint of(IPEndPoint ipEndPoint)
        {
            return new EndPoint {_isHost = false, _i = ipEndPoint};
        }

        public IPEndPoint GetIPEndPoint()
        {
            return _isHost ? _h.GetIPEndPoint() : _i;
        }
    }
}