using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace BQJX.Common
{

    [AddINotifyPropertyChangedInterface]
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
        /// 试管状态  样品管  萃取管  净化管  西林瓶  小瓶
        /// 同一个变量  后面的处理会影响到前面的处理
        /// 对应0-1
        /// </summary>
        public int SampleTubeStatus { get; set; }

        /// <summary>
        /// 萃取管状态
        /// </summary>
        public int PolishStatus { get; set; }

        /// <summary>
        /// 净化管状态
        /// </summary>
        public int PurifyStatus { get; set; }

        /// <summary>
        /// 西林瓶状态
        /// </summary>
        public int SeilingStatus { get; set; }

        /// <summary>
        /// 小瓶状态
        /// </summary>
        public int BottleStatus { get; set; }









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
