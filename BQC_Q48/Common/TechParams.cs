using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public class TechParams
    { 
        /// <summary>
        /// 溶剂A添加量
        /// </summary>
        public double Solvent_A { get; set; }

        /// <summary>
        /// 溶剂B添加量
        /// </summary>
        public double Solvent_B { get; set; }

        /// <summary>
        /// 溶剂C添加量
        /// </summary>
        public double Solvent_C { get; set; }

        /// <summary>
        /// 溶剂D添加量
        /// </summary>
        public double Solvent_D { get; set; }

        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int VortexTime { get; set; }

        /// <summary>
        /// 均质子添加量
        /// </summary>
        public int Junzhizi { get; set; }

        /// <summary>
        /// 超声时间
        /// </summary>
        public int UltrasoundTime { get; set; }

        /// <summary>
        /// 均质时间
        /// </summary>
        public int HomoTime { get; set; }

        /// <summary>
        /// 均质速度
        /// </summary>
        public int HomoVelocity { get; set; }

        /// <summary>
        /// 离心速度
        /// </summary>
        public int CentrifugalVelocity { get; set; }

        /// <summary>
        /// 离心时间
        /// </summary>
        public int CentrifugalTime { get; set; }

        /// <summary>
        /// 上清液提取量
        /// </summary>
        public double ExtractVolume { get; set; }

        /// <summary>
        /// 处理工艺（处理方法）
        /// </summary>
        public int Tech { get; set; }

        /// <summary>
        /// 工艺创建时间
        /// </summary>
        public DateTime Createtime { get; set; }
    }
}
