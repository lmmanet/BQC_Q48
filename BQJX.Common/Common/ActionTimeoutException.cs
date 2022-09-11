using System;
using System.Runtime.Serialization;

namespace BQJX.Common.Common
{
    public class ActionTimeoutException : Exception
    {
        public ActionTimeoutException()
        {

        }

        public ActionTimeoutException(string message) : base(message)
        {

        }

        public ActionTimeoutException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected ActionTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
