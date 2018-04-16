//-----------------------------------------------------------------------------
// <copyright file="UdpSegmentEmitter.cs" company="Amazon.com">
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

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Amazon.Runtime.Internal.Util;
using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Internal.Emitters
{
    /// <summary>
    /// Send the segment to daemon
    /// </summary>
    public class UdpSegmentEmitter : ISegmentEmitter
    {
        /// <summary>
        /// The environment variable for daemon address
        /// </summary>
        public const string EnvironmentVariableDaemonAddress = "AWS_XRAY_DAEMON_ADDRESS";

        private static readonly Logger _logger = Logger.GetLogger(typeof(UdpSegmentEmitter));
        private readonly int _defaultDaemonPort = 2000;
        private readonly IPAddress _defaultDaemonAddress = IPAddress.Loopback;
        private readonly ISegmentMarshaller _marshaller;
        private readonly UdpClient _udpClient;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpSegmentEmitter"/> class.
        /// </summary>
        public UdpSegmentEmitter() : this(new JsonSegmentMarshaller())
        {
        }

        private UdpSegmentEmitter(ISegmentMarshaller marshaller)
        {
            _marshaller = marshaller;
            _udpClient = new UdpClient();

            SetEndPointOrDefault(Environment.GetEnvironmentVariable(EnvironmentVariableDaemonAddress));
        }

        /// <summary>
        /// Gets the end point to daemon.
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// Send segment to local daemon
        /// </summary>
        /// <param name="segment">The segment to be sent</param>
        public void Send(Entity segment)
        {
            try
            {
                var packet = _marshaller.Marshall(segment);
                var data = Encoding.ASCII.GetBytes(packet);
                _udpClient.Send(data, data.Length, EndPoint);
            }
            catch (SocketException e)
            {
                _logger.Error(e, "Failed to send package through socket.");
            }
            catch (ArgumentNullException e)
            {
                _logger.Error(e, "The udp data gram is null.");
            }
            catch (ObjectDisposedException e)
            {
                _logger.Error(e, "The udp client is already closed.");
            }
            catch (InvalidOperationException e)
            {
                _logger.Error(e, "The udp client connection is invalid.");
            }
        }

        /// <summary>
        /// Sets the daemon address.
        /// The daemon address should be in format "IPAddress:Port", i.e. "127.0.0.1:2000"
        /// </summary>
        /// <param name="daemonAddress">The daemon address.</param>
        public void SetDaemonAddress(string daemonAddress)
        {
            if (Environment.GetEnvironmentVariable(EnvironmentVariableDaemonAddress) == null)
            {
                SetEndPointOrDefault(daemonAddress);
            }
            else
            {
                _logger.InfoFormat("Ignoring call to setDaemonAddress as " + EnvironmentVariableDaemonAddress +
                                   " is set.");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_udpClient != null)
                {
#if NET45
                    _udpClient.Close();
#else
                    _udpClient.Dispose();
#endif
                }

                _disposed = true;
            }
        }

        private void SetEndPointOrDefault(string daemonAddress)
        {
            if (string.IsNullOrEmpty(daemonAddress))
            {
                EndPoint = new IPEndPoint(_defaultDaemonAddress, _defaultDaemonPort);
                _logger.InfoFormat("Using default daemon address: {0}:{1}", EndPoint.Address.ToString(), EndPoint.Port);
            }
            else if (IPEndPointExtension.TryParse(daemonAddress, out var daemonEndPoint))
            {
                EndPoint = daemonEndPoint;
            }
            else if (HostEndpoint.TryParse(daemonAddress, out var hostEndpoint))
            {
                try
                {
                    EndPoint = hostEndpoint.GetIPEndPoint();
                }
                catch (SocketException)
                {
                    EndPoint = new IPEndPoint(_defaultDaemonAddress, _defaultDaemonPort);
                    _logger.InfoFormat("The given daemonAddress ({0}) is invalid, using default daemon address {1}:{2}.",
                        daemonAddress, EndPoint.Address.ToString(), EndPoint.Port);
                }
            }
            else
            {
                EndPoint = new IPEndPoint(_defaultDaemonAddress, _defaultDaemonPort);
                _logger.InfoFormat("The given daemonAddress ({0}) is invalid, using default daemon address {1}:{2}.",
                    daemonAddress, EndPoint.Address.ToString(), EndPoint.Port);
            }
        }
    }
}