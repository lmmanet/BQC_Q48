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
        /// 样品1编号
        /// </summary>
        public string SnNum1 { get; set; }

        /// <summary>
        /// 样品2编号
        /// </summary>
        public string SnNum2 { get; set; }

        /// <summary>
        /// 样品1名称
        /// </summary>
        public string Name1 { get; set; }

        /// <summary>
        /// 样品2名称
        /// </summary>
        public string Name2 { get; set; }

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
        /// 样品处理主步骤
        /// <see cref="TechStepStatus"/>
        /// </summary>
        public int MainStep { get; set; }

        /// <summary>
        /// 样品处理子步骤
        /// </summary>
        public int SubStep { get; set; }

        /// <summary>
        /// 搬运2移取上清液步骤
        /// </summary>
        public int BottleStep { get; set; }

        /// <summary>
        /// 搬运西林瓶步骤
        /// </summary>
        public int SeilingStep { get; set; }

        /// <summary>
        /// 离心搬运步骤
        /// </summary>
        public int CenCarrierStep { get; set; }

        /// <summary>
        /// 移液步骤1
        /// </summary>
        public int PipettorStep1 { get; set; } = 1;  
        
        /// <summary>
        /// 移液步骤2
        /// </summary>
        public int PipettorStep2 { get; set; } = 1;

        /// <summary>
        /// 回湿最终时间
        /// </summary>
        public DateTime WetBackEndTime { get; set; }

        /// <summary>
        /// 样品1浓缩失败
        /// </summary>
        public bool ConcentrationFailure { get; set; }

        /// <summary>
        /// 样品2浓缩失败
        /// </summary>
        public bool ConcentrationFailure2 { get; set; }










        /// <summary>
        /// 回调方法
        /// </summary>
        public string ActionCallBack { get; set; }

        /// <summary>
        /// 冰浴ID
        /// </summary>
        public ushort ColdId { get; set; }




    }
}
