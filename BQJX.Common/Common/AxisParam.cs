using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Common
{
    public class AxisParam
    {
        /// <summary>
        /// 点动模式 0：连续模式 1：步进模式
        /// </summary>
        public int JogMode { get; set; }
        /// <summary>
        /// 点动加减速模式设定
        /// </summary>
        public double JogCurve { get; set; }
        /// <summary>
        /// 点动加速度mm/s²
        /// </summary>
        public double JogAcc { get; set; }
        /// <summary>
        /// 点动减速度mm/s²
        /// </summary>
        public double JogDec { get; set; }
        /// <summary>
        /// 回原点加减速模式设定
        /// </summary>
        public double HomeCurve { get; set; }
        /// <summary>
        /// 回原点EZ信号设定 0:不使能 1：使能
        /// </summary>
        public int HomeEza { get; set; }
        /// <summary>
        /// 原点位置值
        /// </summary>
        public double HomePos { get; set; }
        /// <summary>
        /// 回原点模式
        /// </summary>
        public int HomeMode { get; set; }
        /// <summary>
        /// 回原点方向
        /// </summary>
        public int HomeDir { get; set; }
        /// <summary>
        /// 加减速度
        /// </summary>
        public double HomeAcc { get; set; }
        /// <summary>
        /// 爬行速度
        /// </summary>
        public double HomeVo { get; set; }
        /// <summary>
        /// 最大速度
        /// </summary>
        public double HomeVelMax { get; set; }
        /// <summary>
        /// 回原点偏移
        /// </summary>
        public double HomeShift { get; set; }
        /// <summary>
        /// 加速度
        /// </summary>
        public double MoveAcc { get; set; }
        /// <summary>
        /// 减速度
        /// </summary>
        public double MoveDec { get; set; }
        /// <summary>
        /// 停止减速度
        /// </summary>
        public double StopDec { get; set; }
        /// <summary>
        /// 定位曲线因子 0：T型 1：S型
        /// </summary>
        public double SF { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double Curve { get; set; }

    }
}
