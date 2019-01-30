//-----------------------------------------------------------------------------
// <copyright file="IPEndPointExceptionTests.cs" company="Amazon.com">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class HostEndPointTests
    {
        private readonly string[] _badHosts = {null, "i.am.a.very.long.host.name.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler.filler"};
        private readonly int[] _badPorts = {-1, int.MaxValue, int.MinValue, 65536};

        [TestMethod]
        public void SingleThreadGoodHostGoodPort()
        {
            var h = new HostEndPoint("example.com", 2000);
            var ip = h.GetIPEndPoint(out _);
            
            Assert.IsNotNull(ip);
        }

        [TestMethod]
        public void SingleThreadBadHostGoodPort()
        {
            foreach (var host in _badHosts)
            {
                var h = new HostEndPoint(host, 2000);
                Assert.IsNull(h.GetIPEndPoint(out _));
            }
        }
        
        [TestMethod]
        public void SingleThreadGoodHostBadPort()
        {
            foreach (var port in _badPorts)
            {
                var h = new HostEndPoint("example.com", port);
                Assert.IsNull(h.GetIPEndPoint(out _));
            }
        }

        [TestMethod]
        public void SingleThreadBadHostBadPort()
        {
            foreach (var host in _badHosts)
            {
                foreach (var port in _badPorts)
                {
                    var h = new HostEndPoint(host, port);
                    Assert.IsNull(h.GetIPEndPoint(out _));
                }
            }
        }

        [TestMethod]
        public void AvgPerformanceSingleThreadGoodHostGoodPort()
        {
            const int timesToTest = 10_000;
            const double maxMillisecondsPerTest = 0.005; 
            var h = new HostEndPoint("example.com", 2000);
            
            h.GetIPEndPoint(out _); // Call to initialise cache

            var startTime = DateTime.Now;
            for (var i = 0; i < timesToTest; i++)
            {
                h.GetIPEndPoint(out _);
            }

            var finnishTime = DateTime.Now;

            var totalTime = finnishTime.Subtract(startTime).TotalMilliseconds; 
            
            Assert.IsTrue(totalTime < timesToTest * maxMillisecondsPerTest);
        }

        [TestMethod]
        public void TestMultiThreadedUpdateSpacingWithDifferentTtl()
        {
            int[] ttls = {1, 4};
            foreach (var ttl in ttls)
            {
                TestMultiThreadedUpdateSpacing(ttl);
            }
        }
        
        private void TestMultiThreadedUpdateSpacing(int ttl)
        {
            var targetRuntime = ttl * 1;
            const int numberOfThreads = 20;

            var l = new Mutex();
            DateTime timeOfLastUpdate;
            
            var h = new HostEndPoint("example.com", 2000, ttl);
            h.GetIPEndPoint(out _);
            var start = DateTime.Now;
            timeOfLastUpdate = start;
            
            var proc = new ThreadStart(() =>
            {
                while(DateTime.Now.Subtract(start).TotalSeconds < targetRuntime)
                {
                    Assert.IsNotNull(h.GetIPEndPoint(out var updatePerformed));

                    if (!updatePerformed) continue;
                    
                    var now = DateTime.Now;
                    DateTime lastUpdated;

                    l.WaitOne();
                    lastUpdated = timeOfLastUpdate;
                    timeOfLastUpdate = now;
                    l.ReleaseMutex();

                    var timeBetweenUpdates = now.Subtract(lastUpdated).TotalSeconds;
                    Console.WriteLine(timeBetweenUpdates);
                    
                    Assert.IsTrue(timeBetweenUpdates > ttl, string.Format("Frequency of cache updates exceeded, expected at most one every {0} seconds, but two occured with {1} seconds apart!", ttl, timeBetweenUpdates));
                }
            });

            var threads = new Thread[numberOfThreads];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(proc);
            }
            Console.WriteLine("Time between cache updates with cache ttl = {0}", ttl);
            foreach (var t in threads)
            {
                t.Start();
            }
            foreach (var t in threads)
            {
                t.Join();
            }
        }
    }
}