//-----------------------------------------------------------------------------
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
                _logger.Error(e, "Failed to parse IPEndPoint because of matach timeout. ({0})", input);
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
                _logger.InfoFormat("Using custom daemon address: {0}:{1}", endPoint.Address.ToString(), endPoint.Port);
                return true;
            }
            catch (ArgumentOutOfRangeException e)
            {
                _logger.Error(e, "Failed to parse IPEndPoint because argument to IPEndPoint is invalid. ({0}", input);
                return false;
            }
        }
    }
}
