using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public class TechParamsInfo
    {

        /// <summary>
        /// 工艺id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 样品处理工艺名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 加水量
        /// </summary>
        public double AddWater { get; set; }

        /// <summary>
        /// 乙腈添加量
        /// </summary>
        public double ACE { get; set; }

        /// <summary>
        /// 醋酸添加量
        /// </summary>
        public double Acid { get; set; }

        /// <summary>
        /// 甲酸添加量
        /// </summary>
        public double Formic { get; set; }

        /// <summary>
        /// 加均质子量
        /// </summary>
        public double Homo { get; set; }

        /// <summary>
        /// 硫酸镁
        /// </summary>
        public double MgSO4 { get; set; }

        /// <summary>
        /// 氯化钠/硫酸钠
        /// </summary>
        public double NaCl { get; set; }

        /// <summary>
        /// 柠檬酸钠
        /// </summary>
        public double Trisodium { get; set; }

        /// <summary>
        /// 柠檬酸氢二钠
        /// </summary>
        public double Monosodium { get; set; }

        /// <summary>
        /// 乙酸钠
        /// </summary>
        public double Sodium { get; set; }

        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int VortexTime { get; set; }

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int VortexVel { get; set; }

        /// <summary>
        /// 振荡时间
        /// </summary>
        public int VibrationTime { get; set; }

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int VibrationVel { get; set; }

        /// <summary>
        /// 离心时间
        /// </summary>
        public int CentrifugalTime { get; set; }

        /// <summary>
        /// 离心速度
        /// </summary>
        public int CentrifugalVel { get; set; }

        /// <summary>
        /// 上清液提取量
        /// </summary>
        public double ExtractVolume { get; set; }

        /// <summary>
        /// 浓缩时间
        /// </summary>
        public int ConcentrationTime { get; set; }

        /// <summary>
        /// 浓缩速度
        /// </summary>
        public int ConcentrationVel { get; set; }


        /// <summary>
        /// 工艺创建时间
        /// </summary>
        public DateTime Createtime { get; set; }


        
    }
}
