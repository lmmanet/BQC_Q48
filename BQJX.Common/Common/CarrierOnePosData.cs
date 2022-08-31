using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class CarrierOnePosData
    {
        [PosName("50ml试管架1位")]
        public double[] SamplePos1 { get; set; } = new double[3];

        [PosName("50ml试管架2位")]
        public double[] SamplePos2 { get; set; } = new double[3];

        [PosName("50ml试管架3位")]
        public double[] SamplePos3 { get; set; } = new double[3];

        [PosName("50ml试管架4位")]
        public double[] SamplePos4 { get; set; } = new double[3];

        [PosName("冰浴位")]
        public double[] ColdPos { get; set; } = new double[3];

        [PosName("加固接驳位")]
        public double[] AddSolidPos { get; set; } = new double[3];

        [PosName("拧盖1接驳位")]
        public double[] CapperOnePos { get; set; } = new double[3];

        [PosName("涡旋接驳位")]
        public double[] VortexPos { get; set; } = new double[3];

        [PosName("拧盖2接驳位")]
        public double[] CapperTwoPos { get; set; } = new double[3];

        [PosName("振荡接驳位")]
        public double[] VibratioOnePos { get; set; } = new double[3];

        [PosName("移栽接驳位")]
        public double[] TransferLeftPos { get; set; } = new double[3];



        [PosName("枪头位",Is_Z2_Axis = true)]
        public double[] NeedlePos { get; set; } = new double[3];

        [PosName("移液取液位", Is_Z2_Axis = true)]
        public double[] PipettingSourcePos { get; set; } = new double[3];

        [PosName("移液吐液位", Is_Z2_Axis = true)]
        public double[] PipettingTargetPos { get; set; } = new double[3];

        
      
    }
}
