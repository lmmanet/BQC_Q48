using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class OccupyMethodException : Exception
    {
        public OccupyMethodException()
        {

        }

        public OccupyMethodException(string message) : base(message)
        {

        }

        public OccupyMethodException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected OccupyMethodException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
    }
}
