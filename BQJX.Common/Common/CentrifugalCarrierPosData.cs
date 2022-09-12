using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Common
{
    public class CentrifugalCarrierPosData
    {
        [PosName("X左侧接驳位")]
        public double LeftPos { get; set; }

        [PosName("X右侧接驳位")]
        public double RightPos { get; set; }

        [PosName("Z取料位",Is_Z2_Axis = true)]
        public double ZGetPos { get; set; }

        [PosName("Z放料位", Is_Z2_Axis = true)]
        public double ZPutPos { get; set; }

        [PosName("C左侧接驳位1",Is_RotateAxis = true)]
        public double CLeftPutPos1 { get; set; }

        [PosName("C左侧接驳位2", Is_RotateAxis = true)]
        public double CLeftPutPos2 { get; set; }

        [PosName("C左侧接驳位3", Is_RotateAxis = true)]  
        public double CLeftPutPos3 { get; set; }

        [PosName("C左侧接驳位4", Is_RotateAxis = true)]  
        public double CLeftPutPos4 { get; set; }

        [PosName("C中间大管接驳位", Is_RotateAxis = true)]
        public double CCentPos1 { get; set; }

        [PosName("C中间小管接驳位", Is_RotateAxis = true)]
        public double CCentPos2 { get; set; }

        [PosName("X中间接驳位1")] 
        public double XCentPos1{ get; set; }

        [PosName("X中间接驳位2")]
        public double XCentPos2 { get; set; }

        [PosName("C右侧接驳位1", Is_RotateAxis = true)]
        public double CRightPos1 { get; set; }

        [PosName("C右侧接驳位2", Is_RotateAxis = true)]
        public double CRightPos2 { get; set; }

        [PosName("C右侧接驳位3", Is_RotateAxis = true)] 
        public double CRightPos3 { get; set; }

        [PosName("C右侧接驳位4", Is_RotateAxis = true)] 
        public double CRightPos4 { get; set; }

    }
}
