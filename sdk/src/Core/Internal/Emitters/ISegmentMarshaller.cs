//-----------------------------------------------------------------------------
// <copyright file="ISegmentMarshaller.cs" company="Amazon.com">
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

using Amazon.XRay.Recorder.Core.Internal.Entities;

namespace Amazon.XRay.Recorder.Core.Internal.Emitters
{
    /// <summary>
    /// Interface to marshall segment
    /// </summary>
    public interface ISegmentMarshaller
    {
        /// <summary>
        /// Marshalls the segment into a byte[]
        /// </summary>
        /// <param name="segment">The segment to marshall</param>
        /// <returns>The result byte array</returns>
        string Marshall(Entity segment);
    }
}
