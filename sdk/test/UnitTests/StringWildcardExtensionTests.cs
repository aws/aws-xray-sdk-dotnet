//-----------------------------------------------------------------------------
// <copyright file="StringWildcardExtensionTests.cs" company="Amazon.com">
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
using System.Text;
using Amazon.XRay.Recorder.Core.Internal.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Amazon.XRay.Recorder.UnitTests
{
    [TestClass]
    public class StringWildcardExtensionTests
    {
        [TestMethod]
        public void TestInvalidArgs()
        {
            Assert.IsFalse(string.Empty.WildcardMatch(null));
            Assert.IsFalse("whatever".WildcardMatch(string.Empty));
        }

        [TestMethod]
        public void TestMatchExactPositive()
        {
            string pat = "foo";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestMatchExactNegative()
        {
            string pat = "foo";
            string str = "bar";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestSingleWildcardPositive()
        {
            string pat = "fo?";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestSingleWildcardNegative()
        {
            string pat = "f?o";
            string str = "boo";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestMultipleWildcardPositive()
        {
            string pat = "?o?";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestMultipleWildcardNegative()
        {
            string pat = "f??";
            string str = "boo";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobPositive()
        {
            string pat = "*oo";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobPositiveZeroOrMore()
        {
            string pat = "foo*";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobNegativeZeroOrMore()
        {
            string pat = "foo*";
            string str = "fo0";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobNegative()
        {
            string pat = "fo*";
            string str = "boo";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobAndSinglePositive()
        {
            string pat = "*o?";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestGlobAndSingleNegative()
        {
            string pat = "f?*";
            string str = "boo";
            Assert.IsFalse(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void TestPureWildcard()
        {
            string pat = "*";
            string str = "foo";
            Assert.IsTrue(str.WildcardMatch(pat));
        }

        [TestMethod]
        public void ExactMatch()
        {
            Assert.IsTrue("6543210".WildcardMatch("6543210"));
        }

        [TestMethod]
        public void TestMisc()
        {
            string animal1 = "?at";
            string animal2 = "?o?se";
            string animal3 = "*s";

            string vehicle1 = "J*";
            string vehicle2 = "????";

            Assert.IsTrue("bat".WildcardMatch(animal1));
            Assert.IsTrue("cat".WildcardMatch(animal1));
            Assert.IsTrue("horse".WildcardMatch(animal2));
            Assert.IsTrue("mouse".WildcardMatch(animal2));
            Assert.IsTrue("dogs".WildcardMatch(animal3));
            Assert.IsTrue("horses".WildcardMatch(animal3));

            Assert.IsTrue("Jeep".WildcardMatch(vehicle1));
            Assert.IsTrue("ford".WildcardMatch(vehicle2));
            Assert.IsFalse("chevy".WildcardMatch(vehicle2));
            Assert.IsTrue("cAr".WildcardMatch("*"));

            Assert.IsTrue("/bar/foo".WildcardMatch("*/foo"));
        }

        [TestMethod]
        public void TestCaseInsensitivity()
        {
            Assert.IsTrue("Foo".WildcardMatch("Foo", false));
            Assert.IsTrue("Foo".WildcardMatch("Foo", true));

            Assert.IsFalse("FOO".WildcardMatch("Foo", false));
            Assert.IsTrue("FOO".WildcardMatch("Foo", true));

            Assert.IsTrue("Foo0".WildcardMatch("Fo*", false));
            Assert.IsTrue("Foo0".WildcardMatch("Fo*", true));

            Assert.IsFalse("FOo0".WildcardMatch("Fo*", false));
            Assert.IsTrue("FOO0".WildcardMatch("Fo*", true));

            Assert.IsTrue("Foo".WildcardMatch("Fo?", false));
            Assert.IsTrue("Foo".WildcardMatch("Fo?", true));

            Assert.IsFalse("FOo".WildcardMatch("Fo?", false));
            Assert.IsTrue("FoO".WildcardMatch("Fo?", false));
            Assert.IsTrue("FOO".WildcardMatch("Fo?", true));
        }

        [TestMethod]
        public void TestLongStrings()
        {
            // This blew out the stack on a recursive version of wildcardMatch
            char[] t = new char[] { 'a', 'b', 'c', 'd' };
            StringBuilder text = new StringBuilder("a");
            Random r = new Random();
            int size = 8192;

            for (int i = 0; i < size; i++)
            {
                text.Append(t[Math.Abs(r.Next()) % t.Length]);
            }

            text.Append("b");

            Assert.IsTrue(text.ToString().WildcardMatch("a*b"));
        }

        [TestMethod]
        public void TestNoGlobs()
        {
            Assert.IsFalse("abc".WildcardMatch("abcd"));
        }

        [TestMethod]
        public void TestEdgeCaseGlobs()
        {
            Assert.IsTrue(string.Empty.WildcardMatch(string.Empty));
            Assert.IsTrue("a".WildcardMatch("a"));
            Assert.IsTrue("a".WildcardMatch("*a"));
            Assert.IsTrue("ba".WildcardMatch("*a"));
            Assert.IsTrue("a".WildcardMatch("a*"));
            Assert.IsTrue("ab".WildcardMatch("a*"));
            Assert.IsTrue("aa".WildcardMatch("a*a"));
            Assert.IsTrue("aba".WildcardMatch("a*a"));
            Assert.IsTrue("aaa".WildcardMatch("a*a"));
            Assert.IsTrue("aa".WildcardMatch("a*a*"));
            Assert.IsTrue("aba".WildcardMatch("a*a*"));
            Assert.IsTrue("aaa".WildcardMatch("a*a*"));
            Assert.IsTrue("aaaaaaaaaaaaaaaaaaaaaaa".WildcardMatch("a*a*"));
            Assert.IsTrue("akljd9gsdfbkjhaabajkhbbyiaahkjbjhbuykjakjhabkjhbabjhkaabbabbaaakljdfsjklababkjbsdabab".WildcardMatch("a*b*a*b*a*b*a*b*a*"));
            Assert.IsFalse("anananahahanahana".WildcardMatch("a*na*ha"));
        }

        [TestMethod]
        public void TestMultiGlobs()
        {
            // Technically, '**' isn't well defined Balsa, but the wildcardMatch should do the right thing with it.
            Assert.IsTrue("a".WildcardMatch("*a"));
            Assert.IsTrue("a".WildcardMatch("**a"));
            Assert.IsTrue("a".WildcardMatch("***a"));
            Assert.IsTrue("a".WildcardMatch("**a*"));
            Assert.IsTrue("a".WildcardMatch("**a**"));

            Assert.IsTrue("ab".WildcardMatch("a**b"));
            Assert.IsTrue("abb".WildcardMatch("a**b"));

            Assert.IsTrue("a".WildcardMatch("*?"));
            Assert.IsTrue("aa".WildcardMatch("*?"));
            Assert.IsTrue("aa".WildcardMatch("*??"));
            Assert.IsFalse("aa".WildcardMatch("*???"));
            Assert.IsTrue("aaa".WildcardMatch("*?"));

            Assert.IsTrue("a".WildcardMatch("?"));
            Assert.IsFalse("a".WildcardMatch("??"));

            Assert.IsTrue("a".WildcardMatch("?*"));
            Assert.IsTrue("a".WildcardMatch("*?"));
            Assert.IsFalse("a".WildcardMatch("?*?"));
            Assert.IsTrue("aa".WildcardMatch("?*?"));
            Assert.IsTrue("a".WildcardMatch("*?*"));

            Assert.IsFalse("a".WildcardMatch("*?*a"));
            Assert.IsTrue("ba".WildcardMatch("*?*a*"));
        }
    }
}
