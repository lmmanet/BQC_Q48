using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core
{
    public class EtherCATMotionException : Exception
    {

        public EtherCATMotionException()
        {

        }

        public EtherCATMotionException(string message) : base(message)
        {

        }

        public EtherCATMotionException(string message, Exception innerException) : base(message, innerException)
        {

        }

        protected EtherCATMotionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

    }
}
