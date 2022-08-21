using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Core
{
    public class TechParam
    {
        /// <summary>
        /// 工艺名
        /// </summary>
        public string TechName { get; set; }

        /// <summary>
        /// A种固体添加量
        /// </summary>
        public double Solid_A { get; set; }

        /// <summary>
        /// B种固体添加量
        /// </summary>
        public double Solid_B { get; set; }

        /// <summary>
        /// C种固体添加量
        /// </summary>
        public double Solid_C { get; set; }

        /// <summary>
        /// D种固体添加量
        /// </summary>
        public double Solid_D { get; set; }

        /// <summary>
        /// E种固体添加量
        /// </summary>
        public double Solid_E { get; set; }

        /// <summary>
        /// F种固体添加量
        /// </summary>
        public double Solid_F { get; set; }

        /// <summary>
        /// A种溶剂添加量
        /// </summary>
        public double Solvent_A { get; set; }

        /// <summary>
        /// B种溶剂添加量
        /// </summary>
        public double Solvent_B { get; set; }

        /// <summary>
        /// C种溶剂添加量
        /// </summary>
        public double Solvent_C { get; set; }

        /// <summary>
        /// D种溶剂添加量
        /// </summary>
        public double Solvent_D { get; set; }

        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int VortexTime { get; set; }

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int VortexVel { get; set; }

        /// <summary>
        /// 回湿时间
        /// </summary>
        public int WetTime { get; set; }

        /// <summary>
        /// 提取振荡时间
        /// </summary>
        public int ExtractVibrationTime { get; set; }

        /// <summary>
        /// 提取振荡速度
        /// </summary>
        public int ExtractVibrationVel { get; set; }

        /// <summary>
        /// 提取离心时间
        /// </summary>
        public int ExtractCentrifugalTime { get; set; }

        /// <summary>
        /// 提取离心速度
        /// </summary>
        public int ExtractCentrifugalVel { get; set; }

        /// <summary>
        /// 提取上清液量
        /// </summary>
        public double ExtractVolume { get; set; }

        /// <summary>
        /// 净化振荡时间
        /// </summary>
        public int PurifyVibrationTime { get; set; }

        /// <summary>
        /// 净化振荡速度
        /// </summary>
        public int PurifyVibrationVel { get; set; }

        /// <summary>
        /// 净化离心时间
        /// </summary>
        public int PurifyCentrifugalTime { get; set; }

        /// <summary>
        /// 净化离心速度
        /// </summary>
        public int PurifyCentrifugalVel { get; set; }

        /// <summary>
        /// 浓缩量
        /// </summary>
        public int ConcentrationVolume { get; set; }

        /// <summary>
        /// 浓缩时间
        /// </summary>
        public int ConcentrationTime { get; set; }

        /// <summary>
        /// 浓缩速度
        /// </summary>
        public int ConcentrationVel { get; set; }

        /// <summary>
        /// 定溶复溶量
        /// </summary>
        public double DingRong { get; set; }

        /// <summary>
        /// 加标量A
        /// </summary>
        public double Standan_A { get; set; }

        /// <summary>
        /// 加标量B
        /// </summary>
        public double Standan_B { get; set; }

        /// <summary>
        /// 加标量C
        /// </summary>
        public double Standan_C { get; set; }

        /// <summary>
        /// 加标量D
        /// </summary>
        public double Standan_D { get; set; }




        /// <summary>
        /// 最终样品液提取量
        /// </summary>
        public double SampleVolume { get; set; }

    }
}
