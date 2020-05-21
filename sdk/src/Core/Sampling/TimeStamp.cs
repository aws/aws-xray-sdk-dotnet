//-----------------------------------------------------------------------------
// <copyright file="TimeStamp.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Utils;
using System;

namespace Amazon.XRay.Recorder.Core.Sampling
{
    /// <summary>
    /// Stores timestamp for the operations in unix seconds.
    /// </summary>
    public class TimeStamp
    {
        public decimal? Time { get; set; }
        public TimeStamp()
        {
        }

        public TimeStamp(DateTime? dateTime)
        {
            if(dateTime != null)
            {
                Time = dateTime.Value.ToUnixTimeSeconds();
            }
        }

        public TimeStamp(decimal? time)
        {
            if (time != null)
            {
                Time =time;
            }
        }

        /// <summary>
        /// Gets current time in unix seconds.
        /// </summary>
        /// <returns>Instance of <see cref="TimeStamp"/>.</returns>
        public static TimeStamp CurrentTime()
        {
            return new TimeStamp(GetUnixSeconds(DateTime.UtcNow)); // return current time in seconds
        }

        /// <summary>
        /// Get current <see cref="DateTime"/>.
        /// </summary>
        /// <returns>current <see cref="DateTime"/>.</returns>
        public static DateTime CurrentDateTime()
        {
            return DateTime.UtcNow;
        }

        internal bool IsGreaterThan(TimeStamp timeStamp)
        {
            return Time > timeStamp.Time;
        }

        internal void CopyFrom(TimeStamp t)
        {
            Time = t.Time;
        }

        internal TimeStamp PlusSeconds(int? interval)
        {
           return new TimeStamp(Time + interval);
        }
        
        internal bool IsAfter(TimeStamp timeStamp)
        {
            return Time > timeStamp.Time;
        }

        private static decimal GetUnixSeconds(DateTime utcNow)
        {
            return decimal.Floor(utcNow.ToUnixTimeSeconds());
        }
    }
}