//-----------------------------------------------------------------------------
// <copyright file="FacadeSegment.cs" company="Amazon.com">
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
using Amazon.XRay.Recorder.Core.Exceptions;
using System;
using System.Collections.Generic;

namespace Amazon.XRay.Recorder.Core.Internal.Entities
{
    /// <summary>
    /// A Facade segment tracks a period of time associated with a computation or action, along with annotations and key / value data.
    /// A set of trace segments all of which share the same tracing ID form a trace. This segment is created in AWS Lambda and only its subsegments are emitted.
    /// NOTE: This class should not be used. Its used internally by the SDK.
    /// </summary>
    /// <seealso cref="Entity" />
    public class FacadeSegment : Segment
    {
        private static readonly String _mutationUnsupportedMessage = "FacadeSegments cannot be mutated.";

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> class.
        /// </summary>
        /// <param name="name">Name of the node or service component.</param>
        /// <param name="traceId">Unique id for the trace.</param>
        /// <param name="parentId">Unique id of the upstream segment.</param>
        public FacadeSegment(string name, string traceId, string parentId = null) : base(name,traceId,parentId)
        {
            Id = parentId;
            RootSegment = this;
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new string Origin
        {
            get
            {
                return null;
            }
            set
            {
                throw new UnsupportedOperationException(_mutationUnsupportedMessage);
            }
        }

        /// <summary>
        /// Unsupported for Facade segment. Returns always false.
        /// </summary>
        public new IDictionary<string, object> Service
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Unsupported for Facade segment. Returns always false.
        /// </summary>
        public new bool IsServiceAdded
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void SetStartTime(decimal timestamp)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void SetEndTime(decimal timestamp)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }
    
        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void AddMetadata(string key, object value)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void AddException(Exception e)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void AddAnnotation(string key, object value)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void SetStartTimeToNow()
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void SetEndTimeToNow()
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated. Returns always null.
        /// </summary>
        public new IDictionary<string, object> Http
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new bool HasFault {
            get
            {
                return false;
            }
            set
            {
                throw new UnsupportedOperationException(_mutationUnsupportedMessage);
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new bool HasError {
            get
            {
                return false;
            }
            set
            {
                throw new UnsupportedOperationException(_mutationUnsupportedMessage);
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new bool IsThrottled
        {
            get
            {
                return false;
            }
            set
            {
                throw new UnsupportedOperationException(_mutationUnsupportedMessage);
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated. Returns always null.
        /// </summary>
        public new IDictionary<string, string> Sql
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated.
        /// </summary>
        /// <exception cref="UnsupportedOperationException">FacadeSegments cannot be mutated.</exception>
        public new void AddMetadata(string nameSpace, string key, object value)
        {
            throw new UnsupportedOperationException(_mutationUnsupportedMessage);
        }

        /// <summary>
        /// Unsupported as Facade segment cannot be mutated. Returns always false.
        /// </summary>
        public new bool IsHttpAdded
        {
            get
            {
               return false;
            }
        }
    }
}
