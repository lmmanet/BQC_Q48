using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class EPG_ClawStatus
    {
        /// <summary>
        /// 夹爪状态
        /// </summary>
        public byte ClawStatus { get; set; }

        /// <summary>
        /// 错误
        /// </summary>
        public byte Falt { get; set; }

        /// <summary>
        /// 手爪位置
        /// </summary>
        public byte Position { get; set; }

        /// <summary>
        /// 速度
        /// </summary>
        public byte Velocity { get; set; }

        /// <summary>
        /// 力矩
        /// </summary>
        public byte Torque { get; set; }

        /// <summary>
        /// 供电电压
        /// </summary>
        public byte Voltage { get; set; }

        /// <summary>
        /// 温度
        /// </summary>
        public byte Temperature { get; set; }


    }
}
