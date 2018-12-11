//-----------------------------------------------------------------------------
// <copyright file="ExceptionSerializationStrategy.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Entities;
using System;
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Interface used to implement custom exception serialization strategy and record <see cref="Exception"/> on <see cref="Cause"/> instance.
    /// </summary>
   public interface ExceptionSerializationStrategy
    {
        /// <summary>
        /// Decribes exception by iterating subsegments and populates list of <see cref="ExceptionDescriptor"/>.
        /// </summary>
        /// <param name="e">The exception to be added</param>
        /// <param name="subsegments">The subsegments to search for existing exception descriptor.</param>
        /// <returns> List of <see cref="ExceptionDescriptor"/></returns>
        List<ExceptionDescriptor> DescribeException(Exception e, IEnumerable<Subsegment> subsegments);
    }
}
