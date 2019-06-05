//-----------------------------------------------------------------------------
// <copyright file="IStreamingStrategy.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Internal.Emitters;

namespace Amazon.XRay.Recorder.Core.Strategies
{
    /// <summary>
    /// Interface of streaming strategy which is used to determine when and how the subsegments will be streamed.
    /// </summary>
    public interface IStreamingStrategy
    {
        /// <summary>
        /// Determines whenther or not the provided segment requires any subsegment streaming.
        /// </summary>
        /// <param name="input">An instance of <see cref="Segment"/>.</param>
        /// <returns>true if the segment should be streamed.</returns>
        bool ShouldStreamSubsegments(Entity entity);

        /// <summary>
        /// Streams subsegments of instance of <see cref="Entity"/>.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/>.</param>
        /// <param name="emitter">Instance if <see cref="ISegmentEmitter"/>.</param>
        void StreamSubsegments(Entity entity, ISegmentEmitter emitter);
    }
}
