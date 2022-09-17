using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using Q_Platform.Common;

namespace Q_Platform.Models
{
    [Tech("GB23200.113-2018")]
    public class FruitTechParamsModel
    {

        [Tech("工艺名称")]
        public string TechName { get; set; } = "GB23200.113-2018";

        [Tech("硫酸镁")]
        public double Solid_A { get; set; } = 4;

        [Tech("氯化钠")]
        public double Solid_B { get; set; } = 1;

        [Tech("柠檬酸钠")]
        public double Solid_C { get; set; } = 1;

        [Tech("柠檬酸氢二钠")]
        public double Solid_D { get; set; } = 0.5;

        [Tech("均质子")]
        public double Solid_E { get; set; } = 1;



        [Tech("水")]
        public double Solvent_A { get; set; } = 0;

        [Tech("乙腈")]
        public double Solvent_B { get; set; } = 10;

        
        [Tech("涡旋混匀")]
        public int VortexTime { get; set; }

        [Tech("柠檬酸钠")]
        public int VortexVel { get; set; }

        [Tech("柠檬酸钠")]
        public int VibrationOneTime { get; set; }

        [Tech("柠檬酸钠")]
        public int VibrationOneVel { get; set; }

        [Tech("柠檬酸钠")]
        public int CentrifugalOneVelocity { get; set; }

        [Tech("柠檬酸钠")]
        public int CentrifugalOneTime { get; set; }

        [Tech("柠檬酸钠")]
        public double ExtractVolume { get; set; }

        [Tech("柠檬酸钠")]
        public int VibrationTwoTime { get; set; }

        [Tech("柠檬酸钠")]
        public int VibrationTwoVel { get; set; }

        [Tech("柠檬酸钠")]
        public int CentrifugalTwoVelocity { get; set; }

        [Tech("柠檬酸钠")]
        public int CentrifugalTwoTime { get; set; }


        [Tech("柠檬酸钠")]
        public double ConcentrationVolume { get; set; }

        [Tech("柠檬酸钠")]
        public int ConcentrationVel { get; set; }

        [Tech("柠檬酸钠")]
        public int ConcentrationTime { get; set; }

        [Tech("柠檬酸钠")]
        public double MyProperty { get; set; }

        [Tech("柠檬酸钠")]
        public double Add_A { get; set; }

        [Tech("柠檬酸钠")]
        public double Add_B { get; set; }



        [Tech("柠檬酸钠")]
        public double ExtractSampleVolume { get; set; }


        [Tech("柠檬酸钠")]
        public int Tech { get; set; }

        [Tech("柠檬酸钠")]
        public DateTime Createtime { get; set; }











    }
}
