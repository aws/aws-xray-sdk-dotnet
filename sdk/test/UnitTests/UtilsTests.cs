//-----------------------------------------------------------------------------
// <copyright file="UtilsTests.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void IsTheUnixTimeCorrectTest()
        {
            DateTime zeroTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(zeroTime.ToUnixTimeSeconds(), 0m);

            DateTime minusOne = new DateTime(1969, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            Assert.AreEqual(minusOne.ToUnixTimeSeconds(), -1m);

            DateTime plusOne = new DateTime(1970, 1, 1, 0, 0, 1, DateTimeKind.Utc);
            Assert.AreEqual(plusOne.ToUnixTimeSeconds(), 1m);

            DateTime time1 = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(time1.ToUnixTimeSeconds(), 1451606400m);

            DateTime time2 = new DateTime(2020, 1, 13, 21, 18, 47, 228, DateTimeKind.Utc);
            Assert.AreEqual(time2.ToUnixTimeSeconds(), 1578950327.228m);
        }

        [TestMethod]
        public void GenerateRandomHexNumberWithOddDigits()
        {
            int digits = 7;
            string hex = ThreadSafeRandom.GenerateHexNumber(digits);
            Assert.AreEqual(hex.Length, digits);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GenerateRandomHexNumberWithNegativeDigits()
        {
            ThreadSafeRandom.GenerateHexNumber(-1);
        }
    }
}
