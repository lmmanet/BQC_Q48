using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class CarrierTwoPosData
    {
        /// <summary>
        /// 净化管
        /// </summary>
        [PosName("净化管架1位")]
        public double[] PurifyTubePos1 { get; set; } = new double[3];

        [PosName("净化管架2位")]
        public double[] PurifyTubePos2 { get; set; } = new double[3];

        [PosName("西林瓶架1位")]
        public double[] SeilingPos1 { get; set; } = new double[3];

        [PosName("西林瓶架2位")]
        public double[] SeilingPos2 { get; set; } = new double[3];

        [PosName("小瓶架1位")]
        public double[] BottlePos1 { get; set; } = new double[3];

        [PosName("小瓶架2位")]
        public double[] BottlePos2 { get; set; } = new double[3];

        [PosName("小瓶架3位")]
        public double[] BottlePos3 { get; set; } = new double[3];

        [PosName("小瓶架4位")]
        public double[] BottlePos4 { get; set; } = new double[3];

        [PosName("拧盖3接驳位")]
        public double[] CapperThreePos { get; set; } = new double[3];

        [PosName("拧盖4接驳位")]
        public double[] CapperFourPos { get; set; } = new double[3];

        [PosName("拧盖5接驳位")]
        public double[] CapperFivePos { get; set; } = new double[3];

        [PosName("振荡2接驳位")]
        public double[] VibrationTwoPos { get; set; } = new double[3];

        [PosName("浓缩接驳位")]
        public double[] ConcentrationPos { get; set; } = new double[3];

        [PosName("称重位")]
        public double[] WeightPos { get; set; } = new double[3];

        [PosName("离心接驳位")]
        public double[] TransferRightPos { get; set; } = new double[3];



        [PosName("枪头位1",Is_Z2_Axis =true)]
        public double[] NeedlePos1 { get; set; } = new double[3];

        [PosName("枪头位2", Is_Z2_Axis = true)]
        public double[] NeedlePos2 { get; set; } = new double[3];

        [PosName("移液取液位", Is_Z2_Axis = true)]
        public double[] PipettingSourcePos { get; set; } = new double[3];

        [PosName("移液吐液位1", Is_Z2_Axis = true)]
        public double[] PipettingTargetPos1 { get; set; } = new double[3];

        [PosName("移液吐液位2", Is_Z2_Axis = true)]
        public double[] PipettingTargetPos2 { get; set; } = new double[3];

        [PosName("移液吐液位3", Is_Z2_Axis = true)]
        public double[] PipettingTargetPos3 { get; set; } = new double[3];


        //加标
        [PosName("加标取液位", HaveNoneZ_Axis = true)]
        public double[] SyringSourcePos { get; set; } = new double[3];

        [PosName("加标吐液位", HaveNoneZ_Axis = true)]
        public double[] SyringTargePos { get; set; } = new double[3];

        [PosName("加标清洗位", HaveNoneZ_Axis = true)]
        public double[] SyringWashPos { get; set; } = new double[3];



    }
}
