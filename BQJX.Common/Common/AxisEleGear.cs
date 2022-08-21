using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Core.Common
{
    public class AxisEleGear
    {
        /// <summary>
        /// 轴号
        /// </summary>
        public ushort AxisNo { get; set; }

        /// <summary>
        /// 电子齿轮比
        /// </summary>
        public double EleGear { get; set; } = 1;

        /// <summary>
        /// 轴名称
        /// </summary>
        public string AxisName { get; set; }

        /// <summary>
        /// 起始速度
        /// </summary>
        public double StartVel { get; set; } = 0;

        /// <summary>
        /// 停止速度
        /// </summary>
        public double StopVel { get; set; } = 0;

        /// <summary>
        /// S段时间（S）
        /// </summary>
        public double S_param { get; set; } = 0.01;

        /// <summary>
        /// 加速时间（S）
        /// </summary>
        public double Tacc { get; set; } = 0.1;

        /// <summary>
        /// 减速时间（S）
        /// </summary>
        public double Tdec { get; set; } = 0.1;

        /// <summary>
        /// 负限位
        /// </summary>
        public double nLimit { get; set; }

        /// <summary>
        /// 正限位
        /// </summary>
        public double pLimit { get; set; }

        /// <summary>
        /// 回零偏移
        /// </summary>
        public double HomeOffset { get; set; } = 0;

    }
}
