//-----------------------------------------------------------------------------
// <copyright file="TraceId.cs" company="Amazon.com">
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
using System.Numerics;
using Amazon.XRay.Recorder.Core.Internal.Utils;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// Provides utilities to manipulate trace id.
    /// </summary>
    public static class TraceId
    {
        // Trace id contains three elements in a dash separated hex encoded string
        //      1. 4 bit version. Initially the only supported value is 1.
        //      2. Time as an integer number of seconds since the Epoch.
        //      3. 96 bit random number.
        //
        // Example:
        //      1-5759e988-bd862e3fe1be46a994272793
        //      | |        |
        //      | |        random number
        //      | epoch
        //      |
        //      version
        private const int Version = 1;
        private const int ElementsCount = 3;
        private const int RandomNumberHexDigits = 24; // 96 bits
        private const int EpochHexDigits = 8; // 32 bits
        private const int VersionDigits = 1;
        private const int TotalLength = RandomNumberHexDigits + EpochHexDigits + VersionDigits + ElementsCount - 1;
        private const char Delimiter = '-';

        /// <summary>
        /// Randomly generate a new trace id
        /// </summary>
        /// <returns>A new random trace id</returns>
        public static string NewId()
        {
            // Get epoch second as 32bit integer
            int epoch = (int)DateTime.UtcNow.ToUnixTimeSeconds();

            // Get a 96 bit random number
            string randomNumber = ThreadSafeRandom.GenerateHexNumber(RandomNumberHexDigits);

            string[] arr = { Version.ToString(CultureInfo.InvariantCulture), epoch.ToString("x", CultureInfo.InvariantCulture), randomNumber };

            // Concatenate elements with dash
            return string.Join(Delimiter.ToString(), arr);
        }

        /// <summary>
        /// Check whether the trace id is valid
        /// </summary>
        /// <param name="traceId">The trace id</param>
        /// <returns>True if the trace id is valid</returns>
        public static bool IsIdValid(string traceId)
        {
            // Is the input valid?
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return false;
            }

            // Is the total length valid?
            if (traceId.Length != TotalLength)
            {
                return false;
            }

            string[] elements = traceId.Split(Delimiter);

            // Is the number of elements valid?
            if (elements.Length != ElementsCount)
            {
                return false;
            }

            // Is the version a valid integer?
            if (!int.TryParse(elements[0], out int idVersion))
            {
                return false;
            }

            // Is the version supportted?
            if (Version != idVersion)
            {
                return false;
            }
 
            var idEpoch = elements[1];
            var idRand = elements[2];

            // Is the size of epoch and random number valid?
            if (idEpoch.Length != EpochHexDigits || idRand.Length != RandomNumberHexDigits)
            {
                return false;
            }

            // Is the epoch a valid 32bit hex number?
            if (!int.TryParse(idEpoch, NumberStyles.HexNumber, null, out _))
            {
                return false;
            }

            // Is the random number a valid hex number?
            if (!BigInteger.TryParse(idRand, NumberStyles.HexNumber, null, out _))
            {
                return false;
            }

            return true;
        }
    }
}
