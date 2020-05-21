﻿//-----------------------------------------------------------------------------
// <copyright file="StringExtension.cs" company="Amazon.com">
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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Amazon.XRay.Recorder.Core.Internal.Utils
{
    /// <summary>
    /// Perform string matching using standard wildcards (globbing pattern).
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Convert a string from the camel case to snake case.
        /// </summary>
        /// <param name="camelCaseStr">The camel case string.</param>
        /// <returns>The converted snake case string.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Camel case start with lower case letter")]
        public static string FromCamelCaseToSnakeCase(this string camelCaseStr)
        {
            camelCaseStr = char.ToLower(camelCaseStr[0], CultureInfo.InvariantCulture) + camelCaseStr.Substring(1);
            string snakeCaseString = Regex.Replace(camelCaseStr, "(?<char>[A-Z])", match => '_' + match.Groups["char"].Value.ToLowerInvariant());
            return snakeCaseString;
        }

        /// <summary>
        /// Match the string with a pattern using standard wildcards (globbing pattern).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <param name="isCaseInsensitive">if set to <c>true</c> [is case insensitive].</param>
        /// <returns><c>true</c> if the text matches the pattern; otherwise, <c>false</c>.</returns>
        public static bool WildcardMatch(this string text, string pattern, bool isCaseInsensitive = true)
        {
            if (pattern == null || text == null)
            {
                return false;
            }

            if (pattern.Length == 0)
            {
                return text.Length == 0;
            }

            if (IsWildcardGlob(pattern))
            {
                return true;
            }

            int i = 0, p = 0, iStar = text.Length, pStar = 0;
            while (i < text.Length)
            {
                if (p < pattern.Length && text[i] == pattern[p])
                {
                    ++i;
                    ++p;
                }
                else if (p < pattern.Length && isCaseInsensitive && char.ToLower(text[i]) == char.ToLower(pattern[p]))
                {
                    ++i;
                    ++p;
                }
                else if (p < pattern.Length && '?' == pattern[p])
                {
                    ++i;
                    ++p;
                }
                else if (p < pattern.Length && pattern[p] == '*')
                {
                    iStar = i;
                    pStar = p++;
                }
                else if (iStar != text.Length)
                {
                    i = ++iStar;
                    p = pStar + 1;
                }
                else
                    return false;
            }

            while (p < pattern.Length && pattern[p] == '*') ++p;
            return p == pattern.Length && i == text.Length;
        }

        /// <summary>
        /// Simples the wildcard match.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>
        ///   <c>true</c> if the text matches the pattern; otherwise, <c>false</c>.
        /// </returns>
        private static bool SimpleWildcardMatch(string text, string pattern)
        {
            int j = 0;
            int patternLength = pattern.Length;
            int textLength = text.Length;
            for (int i = 0; i < patternLength; i++)
            {
                char p = pattern[i];
                if (p == '*')
                {
                    // Presumption for this method is that glob can only occur at end.
                    return true;
                }

                if (p == '?')
                {
                    if (j == text.Length)
                    {
                        return false; // No character to match
                    }

                    j++;
                }
                else
                {
                    if (j >= text.Length || p != text[j])
                    {
                        return false;
                    }

                    j++;
                }
            }

            return j == textLength;
        }

        /// <summary>
        /// Determines whether the passed pattern is "*"
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <returns>
        ///   <c>true</c> if the passed pattern is "*"; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsWildcardGlob(string pattern)
        {
            return pattern.Length == 1 && pattern[0] == '*';
        }

        /// <summary>
        /// Used to match incoming request and sampling rule parameters.
        /// </summary>
        /// <param name="parameterToMatch">Parameter of incoming request.</param>
        /// <param name="ruleParameter">Instance member of sampling rule to match.</param>
        /// <returns>True, if the two parameter matches else false.</returns>
        public static bool IsMatch(string parameterToMatch, string ruleParameter)
        {
            return (string.IsNullOrEmpty(parameterToMatch) || parameterToMatch.WildcardMatch(ruleParameter));
        }
    }
}
