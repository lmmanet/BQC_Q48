using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class StepAxisEleGear
    {

        /// <summary>
        /// 从站地址
        /// </summary>
        public ushort SlaveId { get; set; }

        /// <summary>
        /// 电子齿轮比
        /// </summary>
        public double EleGear { get; set; } = 1;

        /// <summary>
        /// 轴名称
        /// </summary>
        public string AxisName { get; set; }

        /// <summary>
        /// 原点位置
        /// </summary>
        public int HomePos { get; set; } = 0;

        /// <summary>
        /// 回零偏移 脉冲
        /// </summary>
        public int HomeOffset { get; set; } = 0;

        /// <summary>
        /// 回零模式 2：限位回零负向  3：限位回零正向  6：原点负向回零 7：原点正向回零    14：负向力矩回零  15：正向力矩回零
        /// </summary>
        public ushort HomeMode { get; set; } = 2;

        /// <summary>
        /// 回零高速 rpm
        /// </summary>
        public ushort HomeHigh { get; set; } = 50;

        /// <summary>
        /// 回零低速 rpm
        /// </summary>
        public ushort HomeLow { get; set; } = 5;

        /// <summary>
        /// 回零力矩百分比
        /// </summary>
        public ushort HomeTorque { get; set; } = 50;
    }
}
