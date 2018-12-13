//-----------------------------------------------------------------------------
// <copyright file="EndPoint.cs" company="Amazon.com">
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

using System.Net;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// Represents either a IPEndPoint or HostEndPoint.
    /// </summary>
    public class EndPoint
    {
        private HostEndPoint _h;
        private IPEndPoint _i;
        private bool _isHost;

        /// <summary>
        /// Create an EndPoint representing a HostEndPoint.
        /// </summary>
        /// <param name="hostEndPoint">the host endpoint to represent.</param>
        /// <returns></returns>
        public static EndPoint Of(HostEndPoint hostEndPoint)
        {
            return new EndPoint {_isHost = true, _h = hostEndPoint};
        }

        /// <summary>
        /// Create an EndPoint representing an IPEndPoint.
        /// </summary>
        /// <param name="ipEndPoint">the ip endpoint to represent.</param>
        /// <returns></returns>
        public static EndPoint Of(IPEndPoint ipEndPoint)
        {
            return new EndPoint {_isHost = false, _i = ipEndPoint};
        }

        /// <summary>
        /// Gets the ip of the endpoint that is represented.
        /// </summary>
        /// <returns></returns>
        public IPEndPoint GetIPEndPoint()
        {
            return _isHost ? _h.GetIPEndPoint(out _) : _i;
        }
    }
}