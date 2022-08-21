using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.Modbus
{
    public class SerialPortParams
    {
        /// <summary>
        /// 串口号
        /// </summary>
        public string PortName { get; set; }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 校验0：None    1：ODD    2：Even  3:Mark  4:Space
        /// </summary>
        public int Parity { get; set; }

        /// <summary>
        /// 数据位 5 6 7 8 
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位 0:None    1: One    2: Two   3: OnePointFive
        /// </summary>
        public int StopBits { get; set; }

    }
}
