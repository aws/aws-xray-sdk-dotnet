//-----------------------------------------------------------------------------
// <copyright file="Utils.cs" company="Amazon.com">
//      Copyright 2017 Amazon.com, Inc. or its affiliates. All Rights Reserved.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests.Tools
{
    public static class Utils
    {
        public static Stream CreateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static void AssertExceptionExpected(Action action)
        {
            AssertExceptionExpected(action, typeof(Exception));
        }

        public static T AssertExceptionExpected<T>(Action action) where T : Exception
        {
            return AssertExceptionExpected(action, typeof(T)) as T;
        }

        public static Exception AssertExceptionExpected(Action action, Type expectedExceptionType, string expectedExceptionMessage = null)
        {
            try
            {
                action();
                if (expectedExceptionType != null)
                {
                    Assert.Fail("Exception of type " + expectedExceptionType.FullName + " expected but not thrown!");
                }

                Console.WriteLine("Success, no exception expected or thrown");
                return null;
            }
            catch (Exception e)
            {
                if (expectedExceptionType == null)
                {
                    Assert.Fail("No exception expected, but exception thrown: " + e.ToString());
                }

                Type eType = e.GetType();
                if (!expectedExceptionType.IsAssignableFrom(eType))
                {
                    Assert.Fail("Expected exception of type " + expectedExceptionType.FullName + ", but thrown exception is of type " + eType.FullName + " : " + e.Message);
                }
                else if (
                    !string.IsNullOrEmpty(expectedExceptionMessage) &&
                    !string.Equals(expectedExceptionMessage, e.Message, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail("Expected exception message of [" + expectedExceptionType.FullName + "], but thrown exception has message of [" + e.Message + "]");
                }

                Console.WriteLine("Success, expected " + expectedExceptionType.FullName + ", thrown " + eType.FullName + ": " + e.Message);
                return e;
            }
        }

        public static async Task<Exception> AssertExceptionExpectedAsync(Func<Task> func, Type expectedExceptionType, string expectedExceptionMessage = null)
        {
            try
            {
                await func();
                if (expectedExceptionType != null)
                {
                    Assert.Fail("Exception of type " + expectedExceptionType.FullName + " expected but not thrown!");
                }

                Console.WriteLine("Success, no exception expected or thrown");
                return null;
            }
            catch (Exception e)
            {
                if (expectedExceptionType == null)
                {
                    Assert.Fail("No exception expected, but exception thrown: " + e.ToString());
                }

                Type eType = e.GetType();
                if (!expectedExceptionType.IsAssignableFrom(eType))
                {
                    Assert.Fail("Expected exception of type " + expectedExceptionType.FullName + ", but thrown exception is of type " + eType.FullName + " : " + e.Message);
                }
                else if (
                    !string.IsNullOrEmpty(expectedExceptionMessage) &&
                    !string.Equals(expectedExceptionMessage, e.Message, StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Fail("Expected exception message of [" + expectedExceptionType.FullName + "], but thrown exception has message of [" + e.Message + "]");
                }

                Console.WriteLine("Success, expected " + expectedExceptionType.FullName + ", thrown " + eType.FullName + ": " + e.Message);
                return e;
            }
        }

        public static Stream GetResourceStream(string resourceName)
        {
            Assembly assembly = typeof(Utils).Assembly;
            var resource = FindResourceName(resourceName);
            Stream stream = assembly.GetManifestResourceStream(resource);
            return stream;
        }

        public static string GetResourceText(string resourceName)
        {
            using (StreamReader reader = new StreamReader(GetResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        public static string FindResourceName(string partialName)
        {
            return FindResourceName(s => s.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0).SingleOrDefault();
        }

        public static IEnumerable<string> FindResourceName(Predicate<string> match)
        {
            Assembly assembly = typeof(Utils).Assembly;
            var allResources = assembly.GetManifestResourceNames();
            foreach (var resource in allResources)
            {
                if (match(resource))
                {
                    yield return resource;
                }
            }
        }
    }
}
