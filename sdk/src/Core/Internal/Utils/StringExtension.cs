//-----------------------------------------------------------------------------
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

            int patternLength = pattern.Length;
            int textLength = text.Length;
            if (patternLength == 0)
            {
                return textLength == 0;
            }

            if (IsWildcardGlob(pattern))
            {
                return true;
            }

            if (isCaseInsensitive)
            {
                pattern = pattern.ToUpperInvariant();
                text = text.ToUpperInvariant();
            }

            // Infix globs are relatively rare, and the below search is expensive especially when
            // it is used a lot. Check for infix globs and, in their absence, do the simple thing
            int indexOfGlob = pattern.IndexOf('*');
            if (indexOfGlob == -1 || indexOfGlob == patternLength - 1)
            {
                return SimpleWildcardMatch(text, pattern);
            }

            // The res[i] is used to record if there is a match
            // between the first i chars in text and the first j chars in pattern.
            // So will return res[textLength+1] in the end
            // Loop from the beginning of the pattern
            // case not '*': if text[i]==pattern[j] or pattern[j] is '?', and res[i] is true, 
            //   set res[i+1] to true, otherwise false
            // case '*': since '*' can match any globing, as long as there is a true in res before i
            //   all the res[i+1], res[i+2],...,res[textLength] could be true
            bool[] res = new bool[textLength + 1];
            res[0] = true;
            for (int j = 0; j < patternLength; j++)
            {
                char p = pattern[j];
                if (p != '*')
                {
                    for (int i = textLength - 1; i >= 0; i--)
                    {
                        char t = text[i];
                        res[i + 1] = res[i] && (p == '?' || (p == t));
                    }
                }
                else
                {
                    int i = 0;
                    while (i <= textLength && !res[i])
                    {
                        i++;
                    }

                    for (; i <= textLength; i++)
                    {
                        res[i] = true;
                    }
                }

                res[0] = res[0] && p == '*';
            }

            return res[textLength]; 
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
