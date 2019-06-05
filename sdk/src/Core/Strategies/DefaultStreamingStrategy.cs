﻿using Amazon.XRay.Recorder.Core.Internal.Entities;
using Amazon.XRay.Recorder.Core.Internal.Emitters;
using Amazon.XRay.Recorder.Core.Sampling;

namespace Amazon.XRay.Recorder.Core.Strategies
{
    public class DefaultStreamingStrategy : IStreamingStrategy
    {
        /// <summary>
        /// Default max subsegment size to stream for the strategy.
        /// </summary>
        private const long DefaultMaxSubsegmentSize = 100;
        
        /// <summary>
        /// Max subsegment size to stream fot the strategy.
        /// </summary>
        public long MaxSubsegmentSize { get; private set; } = DefaultMaxSubsegmentSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultStreamingStrategy"/> class.
        /// </summary>
        public DefaultStreamingStrategy() : this(DefaultMaxSubsegmentSize)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultStreamingStrategy"/> class.
        /// </summary>
        /// <param name="maxSubsegmentSize"></param>
        public DefaultStreamingStrategy(long maxSubsegmentSize)
        {
            MaxSubsegmentSize = maxSubsegmentSize;
        }

        /// <summary>
        /// Checks whether subsegments of the current instance of  <see cref="Entity"/> should be streamed.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/></param>
        /// <returns>True if the subsegments are streamable.</returns>
        public bool ShouldStreamSubsegments(Entity entity)
        {
            return entity.Sampled == SampleDecision.Sampled && entity.RootSegment != null && entity.RootSegment.Size >= MaxSubsegmentSize;
        }

        /// <summary>
        /// Streams subsegments of instance of <see cref="Entity"/>.
        /// </summary>
        /// <param name="entity">Instance of <see cref="Entity"/>.</param>
        /// <param name="emitter">Instance of <see cref="ISegmentEmitter"/>.</param>
        public void StreamSubsegments(Entity entity, ISegmentEmitter emitter)
        {
            lock (entity.Subsegments)
            {
                foreach (var next in entity.Subsegments)
                {
                    StreamSubsegments(next, emitter);
                }

                entity.Subsegments.RemoveAll(x => x.HasStreamed);
            }

            if (entity is Segment || entity.IsInProgress || entity.Reference > 0 || entity.IsSubsegmentsAdded)
            {
                return;
            }

            Subsegment subsegment = entity as Subsegment;
            subsegment.TraceId = entity.RootSegment.TraceId;
            subsegment.Type = "subsegment";
            subsegment.ParentId = subsegment.Parent.Id;
            emitter.Send(subsegment);
            subsegment.RootSegment.DecrementSize();
            subsegment.HasStreamed = true;
        }
    }
}