
// <copyright file="IPEndPointExtension.cs" company="Amazon.com">
//      Copyright 2017 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Amazon.Runtime.Internal.Util;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// Provides extension function to <see cref="System.Net.IPEndPoint"/>.
    /// </summary>
    public static class IPEndPointExtension
    {
        private const string Ipv4Address = @"^\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}$";
        private static readonly Logger _logger = Logger.GetLogger(typeof(IPEndPointExtension));
        private const char _addressDelimiter = ' '; // UDP and TCP address 
        private const char _addressPortDelimiter = ':';
        private const string _udpKey = "udp";
        private const string _tcpKey = "tcp";

        /// <summary>
        /// Tries to parse a string to <see cref="System.Net.IPEndPoint"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="endPoint">The parsed IPEndPoint</param>
        /// <returns>true if <paramref name="input"/> converted successfully; otherwise, false.</returns>
        public static bool TryParse(string input, out IPEndPoint endPoint)
        {
            endPoint = null;

            try
            {
                // Validate basic format of IPv4 address
                if (!Regex.IsMatch(input, Ipv4Address, RegexOptions.None, TimeSpan.FromMinutes(1)))
                {
                    _logger.InfoFormat("Failed to parse IPEndPoint because input is invalid. ({0})", input);
                    return false;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                _logger.Error(e, "Failed to parse IPEndPoint because of match timeout. ({0})", input);
                return false;
            }

            string[] ep = input.Split(':');
            if (ep.Length != 2)
            {
                _logger.InfoFormat("Failed to parse IPEndpoint because input has not exactly two parts splitting by ':'. ({0})", input);
                return false;
            }

            // Validate IP address is in valid range
            IPAddress ip;
            if (!IPAddress.TryParse(ep[0], out ip))
            {
                _logger.InfoFormat("Failed to parse IPEndPoint because ip address is invalid. ({0})", input);
                return false;
            }

            int port;
            if (!int.TryParse(ep[1], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out port))
            {
                _logger.InfoFormat("Failed to parse IPEndPoint because port is invalid. ({0})", input);
                return false;
            }

            try
            {
                // Validate port number is in valid range
                endPoint = new IPEndPoint(ip, port);
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                _logger.Error(e, "Failed to parse IPEndPoint because argument to IPEndPoint is invalid. ({0}", input);
                return false;
            }
        }
        
        public static bool TryParse(string input, out HostEndPoint hostEndpoint)
        {
            var entries = input.Split(':');
            if (entries.Length != 2)
            {
                _logger.InfoFormat("Failed to parse HostEndPoint because input has not exactly two parts splitting by ':'. ({0})", input);
                hostEndpoint = null;
                return false;
            }
            if (!int.TryParse(entries[1], out var port))
            {
                _logger.InfoFormat("Failed to parse HostEndPoint because port is invalid. ({0})", input);
                hostEndpoint = null;
                return false;
            }
            hostEndpoint = new HostEndPoint(entries[0], port);
            _logger.InfoFormat("Using custom daemon address: {0}:{1}", hostEndpoint.Host, hostEndpoint.Port);
            return true;
        }

        public static bool TryParse(string input, out EndPoint endpoint)
        {
            if (TryParse(input, out IPEndPoint ipEndPoint))
            {
                endpoint = EndPoint.of(ipEndPoint);
                return true;
            }
            else if (TryParse(input, out HostEndPoint hostEndPoint))
            {
                endpoint = EndPoint.of(hostEndPoint);
                return true;
            }
            else
            {
                endpoint = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to parse a string to <see cref="DaemonConfig"/>.
        /// </summary>
        /// <param name="daemonAddress">The input string.</param>
        /// <param name="daemonEndPoint">The parsed <see cref="DaemonConfig"/> instance.</param>
        /// <returns></returns>
        public static bool TryParse(string daemonAddress, out DaemonConfig daemonEndPoint)
        {
            daemonEndPoint = null;

            if (string.IsNullOrEmpty(daemonAddress))
            {
                return false;
            }

            try
            {
              string[] ep = daemonAddress.Split(_addressDelimiter);
              return TryParseDaemonAddress(ep, out daemonEndPoint);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Invalid daemon address. ({0})", daemonAddress);
                return false;
            }
        }

        private static bool TryParseDaemonAddress(string[] daemonAddress, out DaemonConfig endPoint)
        {
            endPoint = null;
            if (daemonAddress.Length == 1)
            {
                return ParseSingleForm(daemonAddress, out endPoint);
            }
            else if (daemonAddress.Length == 2)
            {
                return ParseDoubleForm(daemonAddress, out endPoint);
            }

            return false;
        }

        private static bool ParseSingleForm(string[] daemonAddress, out DaemonConfig endPoint)
        {
            EndPoint udpEndpoint = null;
            endPoint = new DaemonConfig();

            if (TryParse(daemonAddress[0], out udpEndpoint))
            {
                endPoint.UDPEndpoint = udpEndpoint;
                endPoint.TCPEndpoint = udpEndpoint;
                _logger.InfoFormat("Using custom daemon address for UDP and TCP: {0}:{1}", endPoint.UDP_IP_Endpoint.Address.ToString(), endPoint.UDP_IP_Endpoint.Port);
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool ParseDoubleForm(string[] daemonAddress, out DaemonConfig endPoint)
        {
            endPoint = new DaemonConfig();
            EndPoint udpEndpoint = null;
            EndPoint tcpEndpoint = null;
            IDictionary<string, string> addressMap = new Dictionary<string, string>();
            string[] address1 = daemonAddress[0].Split(_addressPortDelimiter); // tcp:127.0.0.1:2000 udp:127.0.0.2:2001
            string[] address2 = daemonAddress[1].Split(_addressPortDelimiter);

            addressMap[address1[0]] = address1[1] + _addressPortDelimiter + address1[2];
            addressMap[address2[0]] = address2[1] + _addressPortDelimiter + address2[2];

            string udpAddress = null;
            string tcpAddress = null;

            udpAddress = addressMap[_udpKey];
            tcpAddress = addressMap[_tcpKey];

            if (TryParse(udpAddress, out udpEndpoint) && TryParse(tcpAddress, out tcpEndpoint))
            {
                endPoint.UDPEndpoint = udpEndpoint;
                endPoint.TCPEndpoint = tcpEndpoint;
                _logger.InfoFormat("Using custom daemon address for UDP {0}:{1} and TCP {2}:{3}", endPoint.UDP_IP_Endpoint.Address.ToString(), endPoint.UDP_IP_Endpoint.Port, endPoint.TCP_IP_Endpoint.Address.ToString(), endPoint.TCP_IP_Endpoint.Port);
                return true;
            }

            return false;
        }
    }
}
