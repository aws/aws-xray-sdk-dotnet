//-----------------------------------------------------------------------------
// <copyright file="ISegmentEmitter.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Internal.Emitters
{
    /// <summary>
    /// Interface of segment emitter
    /// </summary>
    public interface ISegmentEmitter : IDisposable
    {
        /// <summary>
        /// Send the segment to service
        /// </summary>
        /// <param name="segment">Segment to send</param>
        void Send(Entity segment);

        /// <summary>
        /// Sets the daemon address.
        /// The daemon address should be in format "IPAddress:Port", i.e. "127.0.0.1:2000"
        /// </summary>
        /// <param name="daemonAddress">The daemon address.</param>
        void SetDaemonAddress(string daemonAddress);
    }
}
