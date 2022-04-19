//-----------------------------------------------------------------------------
// <copyright file="ThreadsafeRandom.cs" company="Amazon.com">
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
using System.Linq;
using System.Threading;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// An thread safe wrapper for System.Random
    /// </summary>
    public static class ThreadSafeRandom
    {
        private static readonly Random _global = new Random();
        private static readonly ThreadLocal<Random> _local = new ThreadLocal<Random>(() =>
        {
            int seed;
            lock (_global)
            {
                seed = _global.Next();
            }

            return new Random(seed);
        });

        /// <summary>
        /// Generate a random hex number
        /// </summary>
        /// <param name="digits">Digits of the hex number</param>
        /// <returns>The generated hex number</returns>
        public static string GenerateHexNumber(int digits)
        {
            if (digits < 0)
            {
                throw new ArgumentException("Length can't be a negative number.", nameof(digits));
            }

            byte[] bytes = new byte[digits / 2];
            ThreadSafeRandom.NextBytes(bytes);
            string hexNumber = string.Concat(bytes.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)).ToArray());
            if (digits % 2 != 0)
            {
                hexNumber += ThreadSafeRandom.Next(16).ToString("x", CultureInfo.InvariantCulture);
            }

            return hexNumber;
        }

        /// <summary>
        /// Thread safe version of System.Random.NextDouble().
        /// Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public static double NextDouble()
        {
            return _local.Value.NextDouble();
        }

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers</param>
        private static void NextBytes(byte[] buffer)
        {
            _local.Value.NextBytes(buffer);
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum
        /// </summary>
        /// <param name="maxValue">Max value of the random integer</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0, and less than maxValue</returns>
        private static int Next(int maxValue)
        {
            return _local.Value.Next(maxValue);
        }
    }
}
