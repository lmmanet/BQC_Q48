using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication
{
    public class CommunicationException : Exception
    {

        public CommunicationException()
        {

        }

        public CommunicationException(string message) : base(message)
        {

        }

        public CommunicationException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected CommunicationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

    }
}
