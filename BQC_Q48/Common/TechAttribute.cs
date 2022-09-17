using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Common
{
    public class TechAttribute : Attribute
    {
        /// <summary>
        /// 标准名称
        /// </summary>
        public string StandardName { get; set; }

        public TechAttribute(string name)
        {
            StandardName = name;
        }

    }
}
