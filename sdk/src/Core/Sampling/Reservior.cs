//-----------------------------------------------------------------------------
// <copyright file="Reservior.cs" company="Amazon.com">
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
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Thread safe reservior which holds fixed sampling quota, borrowed count and TTL.
    /// </summary>
    public class Reservior
    {
        private TimeStamp _thisSec = new TimeStamp();
        private TimeStamp _refereshedAt = new TimeStamp();
        private int _takenThisSec;
        private int _borrowedThisSec;
        private readonly int _defaultInterval = 10; // 10 seconds
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static readonly Logger _logger = Logger.GetLogger(typeof(Reservior));

        public TimeStamp TTL { get; set; }
        public int? Quota { get; set; }
        public int? Interval { get; set; }

        internal Reservior()
        {
            Quota = null;
            TTL = null;
            Interval = _defaultInterval;
            _refereshedAt = TimeStamp.CurrentTime();
        }

        /// <summary>
        /// If quota is valid, <see cref="ReserviorDecision"/> is either take or no else 1 request/sec is borrowed.
        /// </summary>
        /// <param name="current"> Current timestamp.</param>
        /// <param name="canBorrow">If true, and quota not valid, single request is borrowed in the current sec for the given <see cref="SamplingRule"/>.</param>
        /// <returns>The reservior decision.</returns>
        internal ReserviorDecision BorrowOrTake(TimeStamp current, bool canBorrow)
        {
            _lock.EnterWriteLock();
            try
            {
                CalibrateThisSec(current);
                // Don't borrow if the quota is available and fresh
                if (Quota != null && Quota >= 0 && TTL != null && TTL.Time >= current.Time)
                {
                    if(_takenThisSec >= Quota)
                    {
                        return ReserviorDecision.No;
                    }
                    _takenThisSec += 1;
                    return ReserviorDecision.Take;
                }

                if (canBorrow) // Otherwise try to borrow if the quota is not present or expired.
                {
                    if(_borrowedThisSec >= 1)
                    {
                        return ReserviorDecision.No;
                    }
                    _borrowedThisSec += 1;
                    return ReserviorDecision.Borrow;
                }

                return ReserviorDecision.No;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void CalibrateThisSec(TimeStamp current) // caller has write lock
        {
          if (_thisSec.Time != current.Time)
            {
                _takenThisSec = 0;
                _borrowedThisSec = 0;
                _thisSec = current;
            }
        }

        /// <summary>
        /// Load quota, ttl and interval into current reservior.
        /// </summary>
        /// <param name="t">Instance of <see cref="Target"/>.</param>
        /// <param name="now">Current timestamp.</param>
        internal void LoadQuota(Target t, TimeStamp now)
        {
            _lock.EnterWriteLock();
            try
            {
                if (t.ReserviorQuota != null)
                {
                    Quota = t.ReserviorQuota;
                }

                if (t.TTL != null)
                {
                    TTL = t.TTL;
                }

                if (t.Interval != null)
                {
                    Interval = t.Interval;
                }
                _refereshedAt = now;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal bool ShouldReport(TimeStamp now)
        {
            _lock.EnterReadLock();
            try
            {
                return now.IsAfter(_refereshedAt.PlusSeconds(Interval));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        internal void CopyFrom(Reservior r)
        {
            _lock.EnterWriteLock();
            try
            {
                if (r.Quota != null)
                {
                    Quota = r.Quota.Value;
                }

                if (r.TTL != null)
                {
                    TTL = new TimeStamp();
                    TTL.CopyFrom(r.TTL);
                }

                if (r.Interval != null)
                {
                    Interval = r.Interval.Value;
                }

                if (r._refereshedAt != null)
                {
                    _refereshedAt = new TimeStamp();
                    _refereshedAt.CopyFrom(r._refereshedAt);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}