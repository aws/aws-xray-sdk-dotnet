//-----------------------------------------------------------------------------
// <copyright file="Statistics.cs" company="Amazon.com">
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
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// This class records requests matched, borrowed and sampled count for the given rule.
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// Number of requests matched for the rule.
        /// </summary>
        public int RequestCount { get; set; }

        /// <summary>
        /// Number of requests borrowed for the rule.
        /// </summary>
        public int BorrowCount { get; set; }

        /// <summary>
        /// Number of requests sampled for the rule.
        /// </summary>
        public int SampledCount { get; set; }

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public Statistics()
        {
        }
        public Statistics(int requestCount, int borrowCount, int sampledCount)
        {
            RequestCount = requestCount;
            BorrowCount = borrowCount;
            SampledCount = sampledCount;
        }

        internal void CopyFrom(Statistics statistics)
        {
            _lock.EnterWriteLock();
            try
            {
                RequestCount = statistics.RequestCount;
                BorrowCount = statistics.BorrowCount;
                SampledCount = statistics.SampledCount;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal void IncrementRequestCount()
        {
            _lock.EnterWriteLock();
            try
            {
                RequestCount++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal void IncrementBorrowCount()
        {
            _lock.EnterWriteLock();
            try
            {
                BorrowCount++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal void IncrementSampledCount()
        {
            _lock.EnterWriteLock();
            try
            {
                SampledCount++;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal Statistics GetSnapShot() 
        {
            _lock.EnterReadLock();
            Statistics s = new Statistics();
            try
            {
                s.RequestCount = RequestCount;
                s.BorrowCount = BorrowCount;
                s.SampledCount = SampledCount;
            }
            finally
            {
                _lock.ExitReadLock();
            }
            ResetStats();
            return s;
        }

        private void ResetStats() 
        {
            _lock.EnterWriteLock();
            try
            {
                RequestCount = 0;
                BorrowCount = 0;
                SampledCount = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        internal int GetRequestCount()
        {
            _lock.EnterReadLock();
            try
            {
                return RequestCount;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}