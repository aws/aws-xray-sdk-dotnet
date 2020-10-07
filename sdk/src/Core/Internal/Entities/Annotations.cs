//-----------------------------------------------------------------------------
// <copyright file="Annotations.cs" company="Amazon.com">
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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    ///  key-value pairs that can be queried through GetTraceSummaries. 
    /// </summary>
    [Serializable]
    public class Annotations : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly IDictionary<string, object> _annotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Annotations"/> class.
        /// </summary>
        public Annotations()
        {
            _annotations = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Gets the annotation value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the annotation value to get</param>
        /// <returns>The value associated with the specified key</returns>
        public object this[string key]
        {
            get
            {
                return _annotations[key];
            }
        }

        /// <summary>
        /// Add the specified key and string value as annotation
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The string value of the annotation to add</param>
        public void Add(string key, string value)
        {
            _annotations[key] = value;
        }

        /// <summary>
        /// Add the specified key and 32bit integer value as annotation
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The 32bit integer value of the annotation to add</param>
        public void Add(string key, int value)
        {
            _annotations[key] = value;
        }

        /// <summary>
        /// Add the specified key and double value as annotation
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The double value of the annotation to add</param>
        public void Add(string key, double value)
        {
            _annotations[key] = value;
        }

        /// <summary>
        /// Add the specified key and long value as annotation
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The long value of the annotation to add</param>
        public void Add(string key, long value)
        {
            _annotations[key] = value;
        }

        /// <summary>
        /// Add the specified key and boolean value as annotation
        /// </summary>
        /// <param name="key">The key of the annotation to add</param>
        /// <param name="value">The boolean value of the annotation to add</param>
        public void Add(string key, bool value)
        {
            _annotations[key] = value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through annotations
        /// </summary>
        /// <returns>An enumerator structure for annotations.</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _annotations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
