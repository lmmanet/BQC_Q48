using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BQJX.Common;
using PropertyChanged;

namespace BQJX.Models
{
    [AddINotifyPropertyChangedInterface]
    public class SampleModel
    {
        /// <summary>
        /// 样品位置编号1-48
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// 样品编号
        /// </summary>
        public string SnNum1 { get; set; }

        /// <summary>
        /// 样品编号
        /// </summary>
        public string SnNum2 { get; set; }

        /// <summary>
        /// 样品名称
        /// </summary>
        public string Name1 { get; set; }

        /// <summary>
        /// 样品名称
        /// </summary>
        public string Name2 { get; set; }

        /// <summary>
        /// 样品创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }


        /// <summary>
        /// 处理工艺名称
        /// </summary>
        public string TechName { get; set; }

        /// <summary>
        /// 工艺参数
        /// </summary>
        public TechParams TechParams { get; set; }

        /// <summary>
        /// 起始步骤
        /// </summary>
        public int StartMainStep { get; set; }

        /// <summary>
        /// 样品任务
        /// </summary>
        public Sample Sample { get; set; }

    }
}
