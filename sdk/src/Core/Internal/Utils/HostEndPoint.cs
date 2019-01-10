//-----------------------------------------------------------------------------
// <copyright file="HostEndPoint.cs" company="Amazon.com">
//      Copyright 2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
//
//      Licensed under the Apache License, Version 2.0 (the "License").
//      You may not use this file except in compliance with the License.
//      A copy of the License is located at
//
//      http://aws.amazon.com/apache2.0
//
//      or in the "license" file accompanying this file. This file is distributed
//      on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
//      express or implied. See the License for the specific language governing
//      permissions and limitations under the License.
// </copyright>
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
	/// <summary>
	/// Represents a endpoint on some network.
	/// The represented endpoint is identified by a hostname.
	///
	/// Internally resolves and caches an ip for the hostname.
	/// The ip is cached to keep the normal path speedy and non-blocking.
	/// </summary>
	public class HostEndPoint
	{
		private readonly int _cacheTtl;	//Seconds to consider a cached dns response valid
		private IPEndPoint _ipCache;
		private DateTime? _timestampOfLastIPCacheUpdate;
		private static readonly Logger _logger = Logger.GetLogger(typeof(HostEndPoint));
		private readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

		/// <summary>
		/// Create a HostEndPoint.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="cacheTtl"></param>
		public HostEndPoint(string host, int port, int cacheTtl = 60)
		{
			Host = host;
			Port = port;
			_cacheTtl = cacheTtl;
		}
		
		/// <summary>
		/// Get the hostname that identifies the endpoint.
		/// </summary>
		public string Host { get; }
		/// <summary>
		/// Get the port of the endpoint.
		/// </summary>
		public int Port { get; }


		/// <summary>
		/// Check to see if the cache is valid.
		/// A lock with at least read access MUST be held when calling this method!
		/// </summary>
		/// <returns>true if the cache is valid, false otherwise.</returns>
		private CacheState IPCacheIsValid()
		{
			if (_ipCache == null)
			{
				return CacheState.Invalid;
			}

			if (!(_timestampOfLastIPCacheUpdate is DateTime lastTimestamp))
			{
				return CacheState.Invalid;
			}
			
			if (DateTime.Now.Subtract(lastTimestamp).TotalSeconds < _cacheTtl)
			{
				return CacheState.Valid;
			}

			return CacheState.Invalid;
		}
		
		/// <summary>
		/// Checks to see if the cache is valid.
		/// This method is essentially a wrapper around <see cref="IPCacheIsValid"/> that acquires the required lock.
		/// </summary>
		/// <returns>true if the cache is valid, false otherwise.</returns>
		private CacheState LockedIPCacheIsValid()
		{
			// If !entered => another thread holds a write lock, i.e. updating the cache
			if (!cacheLock.TryEnterReadLock(0)) return CacheState.Updating;
			try
			{	
				return IPCacheIsValid();
			}
			finally
			{
				cacheLock.ExitReadLock();
			}
		}
		
		/// <summary>
		/// Returns a cached ip resolved from the hostname.
		/// If the cached address is invalid this method will try to update it.
		/// The IP address returned is never guaranteed to be valid.
		/// An IP address may be invalid to due to several factors, including but not limited to:
		///  * DNS record is incorrect,
		///  * DNS record might have changed since last update.
		/// The returned IPEndPoint may also be null if no cache update has been successful.
		/// </summary>
		/// <param name="updatePerformed">set to true if an update was performed, false otherwise</param>
		/// <returns>the cached IPEndPoint, may be null</returns>
		public IPEndPoint GetIPEndPoint(out bool updatePerformed)
		{
			// LockedIPCacheIsValid and UpdateCache will in unison perform
			// a double checked locked to ensure:
			// 1. UpdateCache only is called when it appears that the cache is invalid
			// 2. The cache will not be updated when not necessary
			if (LockedIPCacheIsValid() == CacheState.Invalid)
			{
				updatePerformed = UpdateCache();
			}
			else
			{
				updatePerformed = false;
			}
			
			cacheLock.EnterReadLock();
			try
			{
				return _ipCache;
			}
			finally
			{
				cacheLock.ExitReadLock();
			}
		}

		/// <summary>
		/// Updates the cache if invalid.
		/// Utilises an upgradable read lock, meaning only one thread at a time can enter this method.
		/// </summary>
		private bool UpdateCache()
		{
			if (!cacheLock.TryEnterUpgradeableReadLock(0))
			{
				// Another thread is already performing an update => bail and use potentially dirty cache
				return false;
			}
			try
			{
				// We hold a UpgradableReadLock so when may call IPCacheIsValid
				if (IPCacheIsValid() != CacheState.Invalid)
				{
					// Cache no longer invalid, i.e. another thread performed the update after us seeing it invalid
					// and before now.
					return false;
				}
				
				// We have confirmed that the cache still is invalid and needs updating.
				// We know that we are the only ones that may update it because we hold an UpgradeableReadLock
				// Only one thread may hold such lock at a time, see:
				// https://docs.microsoft.com/en-gb/dotnet/api/system.threading.readerwriterlockslim?view=netframework-4.7.2#remarks
				
				var ipEntries = Dns.GetHostAddresses(Host);
				var newIP = ipEntries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

				if (newIP == null)
				{
					_logger.InfoFormat(
						"IP cache invalid: DNS responded with no IP addresses for {1}. Cached IP address not updated!.",
						_ipCache, Host);
					return false;
				}
				// Upgrade our read lock to write mode
				cacheLock.EnterWriteLock();
				try
				{
					_timestampOfLastIPCacheUpdate = DateTime.Now;
					_ipCache = new IPEndPoint(newIP, Port);
					return true;
				}
				//Error catching for IPEndPoint creation
				catch (ArgumentNullException)
				{
					_logger.InfoFormat("IP cache invalid: resolved IP address for hostname {0} null. Cached IP address not updated!", Host);
				}
				catch (ArgumentOutOfRangeException)
				{
					_logger.InfoFormat("IP cache invalid: either the port {0} or IP address {1} is out of range. Cached IP address not updated!", Port, newIP);
				}
				finally
				{
					// Downgrade back to read mode
					cacheLock.ExitWriteLock();
				}
				_logger.InfoFormat("IP cache invalid: updated ip cache for {0} to {1}.", Host, newIP);
			}
			//Error catching for DNS resolve
			catch (ArgumentNullException)
			{
				_logger.InfoFormat(
					"IP cache invalid: failed to resolve DNS due to host being null. Cached IP address not updated!");
			}
			catch (ArgumentOutOfRangeException)
			{
				_logger.InfoFormat(
					"IP cache invalid: failed to resolve DNS due to host being longer than 255 characters. ({0}) Cached IP address not updated!",
					Host);
			}
			catch (SocketException)
			{
				_logger.InfoFormat("IP cache invalid: failed to resolve DNS. ({0}) Cached IP address not updated!", Host);
			}
			catch (ArgumentException)
			{
				_logger.InfoFormat("IP cache invalid: failed to update cache due to {0} not being a valid IP. Cached IP address not updated!", Host);
			}
			finally
			{
				cacheLock.ExitUpgradeableReadLock();
			}

			return false;
		}

		private enum CacheState
		{
			Valid,
			Invalid,
			Updating
		}
	}
}
