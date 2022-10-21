using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    /// <summary>
    /// 样品记录显示
    /// </summary>
    public class SampleInfo
    {
        /// <summary>
        /// 样品编号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 样品1号
        /// </summary>
        public string SnNum{ get; set; }

        /// <summary>
        /// 样品1称
        /// </summary>
        public string Name{ get; set; }

        /// <summary>
        /// 样品处理工艺名
        /// </summary>
        public string TechName { get; set; }


        /// <summary>
        /// 样品处理状态
        /// </summary>
        public int Status { get; set; }


        /// <summary>
        /// 样品创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }


        

    }


}
