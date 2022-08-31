using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{

    [AttributeUsage(AttributeTargets.Property)]
    public class PosNameAttribute:Attribute
    {
        public string PosName { get; set; }

        public bool Is_Z2_Axis { get; set; } = false;

        public bool HaveNoneZ_Axis { get; set; }

        public PosNameAttribute(string name)
        {
            PosName = name;
        }
    }
}
