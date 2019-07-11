using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.XRay.Recorder.Core.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the entity is already emitted.
    /// </summary>
    [Serializable]
    class AlreadyEmittedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AlreadyEmittedException"/> class
        /// </summary>
        public AlreadyEmittedException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyEmittedException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">Error message</param>
        public AlreadyEmittedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyEmittedException"/> class 
        /// with a specified error message and a reference to the inner exception that is 
        /// the cause of this exception.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="inner">Inner exception</param>
        public AlreadyEmittedException(string message, Exception inner) : base(message, inner) { }
    }
}
