using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
	public class HostEndPoint
	{
		private const int CACHE_TTL = 60;	//Seconds to consider a cached dns response valid
		private IPEndPoint _ipCache = null;
		private DateTime? _timestampOfLastIPCacheUpdate = null;
		private static readonly Logger _logger = Logger.GetLogger(typeof(HostEndPoint));

		public HostEndPoint(string host, int port)
		{
			Host = host;
			Port = port;
		}
		
		public string Host { get; private set; }
		public int Port { get; private set; }


		private bool isIPCacheValid()
		{
			if (_ipCache == null)
			{
				return false;
			}

			if (_timestampOfLastIPCacheUpdate is DateTime lastTimestamp)
			{
				return DateTime.Now.Subtract(lastTimestamp).TotalSeconds < CACHE_TTL;
			}
			else
			{
				return false;
			}
		}

		public IPEndPoint GetIPEndPoint()
		{
			if (!isIPCacheValid())
			{
				try
				{
 					var ipEntries = Dns.GetHostAddresses(Host);
					var newIP = ipEntries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
					if (newIP != null)
					{
						_timestampOfLastIPCacheUpdate = DateTime.Now;
						_logger.InfoFormat("IP cache invalid: updated ip cache for {0} to {1}.", Host, newIP);
						_ipCache = new IPEndPoint(newIP, Port);
					}
					else
					{
						_logger.InfoFormat("IP cache invalid: DNS responded with zero IP addresses for {1}. Falling back to cached IP, e.g. {0}.", _ipCache, Host);
					}
				}
				//Error catching for DNS resolve
				catch(ArgumentNullException)
				{
					_logger.InfoFormat("IP cache invalid: failed to resolve DNS due to host being null. Falling back to cached IP, e.g. {0}.", _ipCache);
				}
				catch(ArgumentOutOfRangeException)
				{
					_logger.InfoFormat("IP cache invalid: failed to resolve DNS due to host being longer than 255 characters. ({0})", Host);
				}
				catch(SocketException)
				{
					_logger.InfoFormat("IP cache invalid: failed to resolve DNS. ({0})", Host);
				}
				catch(ArgumentException)
				{
					_logger.InfoFormat("IP cache invalid: failed to update cache due to {0} not being a vaild IP.", Host);
				}
			}

			return _ipCache;
		}
	}
}
