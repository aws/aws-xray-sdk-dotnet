//-----------------------------------------------------------------------------
// <copyright file="RateLimiter.cs" company="Amazon.com">
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
using System.Threading;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Core.Sampling.Local
{
    /// <summary>
    /// The RateLimiter will distribute permit to the first <see cref="LimitPerSecond"/> requests arrives in every epoch second, and block any request that comes later in that second.
    /// </summary>
    public class RateLimiter
    {
        private long _countInLastSecond;
        private long _lastSecond;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimiter"/> class.
        /// </summary>
        /// <param name="limitPerSecond">The number of request that will be permitted every epoch second.</param>
        public RateLimiter(long limitPerSecond)
        {
            this.LimitPerSecond = limitPerSecond;
        }

        /// <summary>
        /// Gets or sets the limit per second.
        /// </summary>
        public long LimitPerSecond { get; set; }

        /// <summary>
        /// Request a single permit from this <see cref="RateLimiter"/>.
        /// </summary>
        /// <returns>A value that indicates whether a permit is successfully acquired.</returns>
        public bool Request()
        {
            long now = (long)decimal.Floor(DateTime.UtcNow.ToUnixTimeSeconds());
            if (now != _lastSecond)
            {
                Interlocked.Exchange(ref _countInLastSecond, 0);
                _lastSecond = now;
            }

            if (Interlocked.Read(ref _countInLastSecond) < LimitPerSecond)
            {
                // In edge case, the count may go above limit, as the increment and read together is not atomic.
                // The potential overrun will be fairly small because the read and increment is close. It is not
                // worth solving at the cost of performance.
                Interlocked.Increment(ref _countInLastSecond);
                return true;
            }

            return false;
        }
    }
}
