//-----------------------------------------------------------------------------
// <copyright file="DaemonConfig.cs" company="Amazon.com">
//      Copyright 2016 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using Amazon.Runtime.Internal.Util;
using System;
using System.Net;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// DaemonConfig stores X-Ray daemon configuration about the ip address and port for UDP and TCP port. It gets the address
    /// string from "AWS_TRACING_DAEMON_ADDRESS" and then from recorder's configuration for "daemon_address".
    /// A notation of '127.0.0.1:2000' or 'tcp:127.0.0.1:2000 udp:127.0.0.2:2001' or 'udp:127.0.0.1:2000 tcp:127.0.0.2:2001'
    /// are both acceptable. The first one means UDP and TCP are running at the same address.
    /// By default it assumes a X-Ray daemon running at 127.0.0.1:2000 listening to both UDP and TCP traffic.
    /// </summary>
    public class DaemonConfig
    {
        private static readonly Logger _logger = Logger.GetLogger(typeof(DaemonConfig));
        /// <summary>
        /// The environment variable for daemon address.
        /// </summary>
        public const string EnvironmentVariableDaemonAddress = "AWS_XRAY_DAEMON_ADDRESS";

        /// <summary>
        /// Default address for daemon.
        /// </summary>
        public const string DefaultAddress = "127.0.0.1:2000";
        private static readonly int _defaultDaemonPort = 2000;
        private static readonly IPAddress _defaultDaemonAddress = IPAddress.Loopback;

        /// <summary>
        /// Default UDP and TCP endpoint.
        /// </summary>
        public static readonly IPEndPoint DefaultEndpoint = new IPEndPoint(_defaultDaemonAddress, _defaultDaemonPort);

        /// <summary>
        /// Gets aor sets UDP endpoint.
        /// </summary>
        public IPEndPoint UDPEndpoint { get; set; }

        /// <summary>
        /// Gets or sets TCP endpoint.
        /// </summary>
        public IPEndPoint TCPEndpoint { get; set; }

        public DaemonConfig()
        {
            UDPEndpoint = DefaultEndpoint;
            TCPEndpoint = DefaultEndpoint;
        }

        internal static DaemonConfig ParsEndpoint(string daemonAddress)
        {
            DaemonConfig daemonEndPoint;
          
            if (!IPEndPointExtension.TryParse(daemonAddress, out daemonEndPoint))
            {
                 daemonEndPoint = new DaemonConfig();
                _logger.InfoFormat("The given daemonAddress ({0}) is invalid, using default daemon UDP and TCP address {1}:{2}.", daemonAddress, daemonEndPoint.UDPEndpoint.Address.ToString(), daemonEndPoint.UDPEndpoint.Port);
            }
            return daemonEndPoint;
        }

        /// <summary>
        /// Parses daemonAddress and sets enpoint. If <see cref="EnvironmentVariableDaemonAddress"/> is set, this call is ignored.
        /// </summary>
        /// <param name="daemonAddress"> Dameon address to be parsed and set to <see cref="DaemonConfig"/> instance.</param>
        /// <returns></returns>
        public static DaemonConfig GetEndPoint(string daemonAddress = null)
        {
            if(Environment.GetEnvironmentVariable(EnvironmentVariableDaemonAddress) != null)
            {
                if (!string.IsNullOrEmpty(daemonAddress))
                {
                    _logger.InfoFormat("Ignoring call to GetEndPoint as " + EnvironmentVariableDaemonAddress + " is set.");
                }
                return ParsEndpoint(Environment.GetEnvironmentVariable(EnvironmentVariableDaemonAddress));
            }
            else
            {
                return ParsEndpoint(daemonAddress);
            }
        }
    }
}
