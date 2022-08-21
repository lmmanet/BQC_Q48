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
        public double[] PurifyTubePos1 { get; set; } = new double[3];
        public double[] PurifyTubePos2 { get; set; } = new double[3];
        public double[] SeilingPos1 { get; set; } = new double[3];
        public double[] SeilingPos2 { get; set; } = new double[3];
        public double[] BottlePos1 { get; set; } = new double[3];
        public double[] BottlePos2 { get; set; } = new double[3];
        public double[] BottlePos3 { get; set; } = new double[3];
        public double[] BottlePos4 { get; set; } = new double[3];
        public double[] CapperThreePos { get; set; } = new double[3];
        public double[] CapperFourPos { get; set; } = new double[3];
        public double[] CapperFivePos { get; set; } = new double[3];
        public double[] VibrationTwoPos { get; set; } = new double[3];
        public double[] ConcentrationPos { get; set; } = new double[3];
        public double[] WeightPos { get; set; } = new double[3];
        public double[] TransferRightPos { get; set; } = new double[3];


        public double[] NeedlePos1 { get; set; } = new double[3];
        public double[] NeedlePos2 { get; set; } = new double[3];
        public double[] PipettingSourcePos { get; set; } = new double[3];
        public double[] PipettingTargetPos1 { get; set; } = new double[3];
        public double[] PipettingTargetPos2 { get; set; } = new double[3];
        public double[] PipettingTargetPos3 { get; set; } = new double[3];


        //加标
        public double[] SyringSourcePos { get; set; } = new double[3];
        public double[] SyringTargePos { get; set; } = new double[3];
        public double[] SyringWashPos { get; set; } = new double[3];



    }
}
