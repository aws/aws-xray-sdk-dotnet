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
		private HostEndPoint(string host, int port)
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
				var ipEntries = Dns.GetHostAddresses(Host);
				var newIP = ipEntries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
				if (newIP != null)
				{
					_timestampOfLastIPCacheUpdate = DateTime.Now;
					_logger.InfoFormat("IP cache invaild: updated ip cache for {0} to {1}.", Host, newIP);
					_ipCache = new IPEndPoint(newIP, Port);
				}
				else
				{
					_logger.InfoFormat("IP cache invaild: DNS responded with zero IP addresses for {1}. Falling back to cached IP, e.g. {0}.", _ipCache, Host);
				}
			}

			return _ipCache;
		}

		public static bool TryParse(string entry, out HostEndPoint hostEndpoint)
		{
			var entries = entry.Split(':');
			if (entries.Length != 2)
			{
				_logger.InfoFormat("Failed to parse HostEndPoint because input has not exactly two parts splitting by ':'. ({0})", entry);
				hostEndpoint = null;
				return false;
			}
			if (!int.TryParse(entries[1], out var port))
			{
				_logger.InfoFormat("Failed to parse HostEndPoint because port is invalid. ({0})", entry);
				hostEndpoint = null;
				return false;
			}
			hostEndpoint = new HostEndPoint(entries[0], port);
			_logger.InfoFormat("Using custom daemon address: {0}:{1}", hostEndpoint.Host, hostEndpoint.Port);
			return true;
		}
	}
}
