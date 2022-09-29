using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public class Sample
    {
        /// <summary>
        /// 样品位置编号1-48
        /// </summary>
        public ushort Id { get; set; }

        /// <summary>
        /// 样品编号
        /// </summary>
        public string SnNum { get; set; }

        /// <summary>
        /// 样品名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 样品创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 样品位置状态 
        /// <see cref="SampleStatus"/>
        /// </summary>
        public Int64 Status { get; set; }

        /// <summary>
        /// 试管状态
        /// 对应0-1
        /// </summary>
        public int TubeStatus { get; set; }

        /// <summary>
        /// 样品处理工艺
        /// </summary>
        public string TechName { get; set; }

        /// <summary>
        /// 样品当前处理进度
        /// </summary>
        public int CurrentProcess { get; set; }

        /// <summary>
        /// 样品工艺参数
        /// </summary>
        public TechParams TechParams { get; set; }

        /// <summary>
        /// 振荡涡旋内部步骤
        /// </summary>
        public int VibrationAndVortexStep { get; set; }

        /// <summary>
        /// 西林瓶重量2
        /// </summary>
        public double SeilingWeight1 { get; set; }

        /// <summary>
        /// 西林瓶重量2
        /// </summary>
        public double SeilingWeight2 { get; set; }

        /// <summary>
        /// 提取样品液步骤
        /// </summary>
        public int ExtractSampleStep { get; set; }

    }
}
