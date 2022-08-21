using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Core
{
    public class SampleObj
    {
        /// <summary>
        /// 样品编号1-48
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 样品名称
        /// </summary>
        public string SampleName { get; set; }


        /// <summary>
        /// 样品状态
        /// </summary>
        public int SampleStatus { get; set; }

        /// <summary>
        /// 样品工艺参数
        /// </summary>
        public TechParam TechParma { get; set; }


    }
}
