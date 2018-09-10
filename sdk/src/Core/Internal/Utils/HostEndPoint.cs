using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
	public class HostEndPoint
	{
		private const int CACHE_TTL = 60;	//Seconds to consider a cached dns response valid
		private IPEndPoint _ipCache = null;
		private DateTime? _timestampOfLastIPCacheUpdate = null;
		private static readonly Logger _logger = Logger.GetLogger(typeof(HostEndPoint));
		private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

		public HostEndPoint(string host, int port)
		{
			Host = host;
			Port = port;
		}
		
		public string Host { get; private set; }
		public int Port { get; private set; }


		private bool isIPCacheValid()
		{
			bool entered = cacheLock.TryEnterReadLock(0);

			if (!entered)
			{
				//Another thread holds a write lock, i.e. updating the cache => the cache is dirty
				return false;
			}

			bool res;
			
			if (_ipCache == null)
			{
				res = false;
			}

			if (_timestampOfLastIPCacheUpdate is DateTime lastTimestamp)
			{
				res = DateTime.Now.Subtract(lastTimestamp).TotalSeconds < CACHE_TTL;
			}
			else
			{
				res = false;
			}
			
			cacheLock.ExitReadLock();
			return res;
		}

		private void updateCache()
		{
			//If we fail with timeout = 0 another thread is already updating the cache and we don't need to it as well
			bool entered = cacheLock.TryEnterReadLock(0);

			if (!entered)
			{
				return;
			}
			
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
					_logger.InfoFormat(
						"IP cache invalid: DNS responded with zero IP addresses for {1}. Falling back to cached IP, e.g. {0}.",
						_ipCache, Host);
				}
			}
			//Error catching for DNS resolve
			catch (ArgumentNullException)
			{
				_logger.InfoFormat(
					"IP cache invalid: failed to resolve DNS due to host being null. Falling back to cached IP, e.g. {0}.",
					_ipCache);
			}
			catch (ArgumentOutOfRangeException)
			{
				_logger.InfoFormat(
					"IP cache invalid: failed to resolve DNS due to host being longer than 255 characters. ({0})",
					Host);
			}
			catch (SocketException)
			{
				_logger.InfoFormat("IP cache invalid: failed to resolve DNS. ({0})", Host);
			}
			catch (ArgumentException)
			{
				_logger.InfoFormat("IP cache invalid: failed to update cache due to {0} not being a vaild IP.", Host);
			}
			finally
			{
				cacheLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// Returns a cached ip resolved from the host.
		///
		/// If the cache is invalid this method will update it.
		///
		/// The cache returned may be dirty. This may occur since the cache has a TTL
		/// but the host-ip mapping may change in that time. It may also occur when
		/// this thread tries to update the cache but another thread is already doing so.
		/// </summary>
		/// <returns></returns>
		public IPEndPoint GetIPEndPoint()
		{
			
			if (!isIPCacheValid())
			{
				updateCache();
			}
			
			return _ipCache;
		}
	}
}
