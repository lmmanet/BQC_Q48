using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace BQJX.Common
{
    [AddINotifyPropertyChangedInterface]
    public class TechParams
    {

        /// <summary>
        /// 0：GB23200.113-2018（果蔬）  1:GB23200.113-2018（坚果） 2：GB23200.121-2021（果蔬）  3：GB23200.121-2021（坚果） 10：兽药
        /// </summary>
        public int TechSpecies { get; set; }

        /// <summary>
        /// 加水量
        /// </summary>

        public double AddWater { get; set; }

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
        /// 加均质子 加溶剂 =》 加盐 =》加水 三组加固数据
        /// </summary>
        public double[] AddHomo { get; set; } = new double[3];

        /// <summary>
        /// 加固B 分两次加固  和溶剂一起  或单独
        /// </summary>
        public double[] Solid_B { get; set; } = new double[3];

        /// <summary>
        /// 加固C 分两次加固  和溶剂一起  或单独
        /// </summary>
        public double[] Solid_C { get; set; } = new double[3];

        /// <summary>
        /// 加固D 分两次加固  和溶剂一起  或单独
        /// </summary>
        public double[] Solid_D { get; set; } = new double[3];

        /// <summary>
        /// 加固E 分两次加固  和溶剂一起  或单独
        /// </summary>
        public double[] Solid_E { get; set; } = new double[3];

        /// <summary>
        /// 加固F 分两次加固  和溶剂一起  或单独
        /// </summary>
        public double[] Solid_F { get; set; } = new double[3];



        /// <summary>
        /// 回湿时间
        /// </summary>
        public int WetTime { get; set; }



        /// <summary>
        /// 涡旋时间
        /// </summary>
        public int[] VortexTime { get; set; } = new int[4];

        /// <summary>
        /// 涡旋速度
        /// </summary>
        public int[] VortexVel { get; set; } = new int[4];

        /// <summary>
        /// 提取振荡1时间
        /// </summary>
        public int[] VibrationOneTime { get; set; } = new int[4];

        /// <summary>
        /// 提取振荡1速度
        /// </summary>
        public int[] VibrationOneVel { get; set; } = new int[4];  
        
        /// <summary>
        /// 提取振荡2时间
        /// </summary>
        public int[] VibrationTwoTime { get; set; } = new int[2];

        /// <summary>
        /// 提取振荡2速度
        /// </summary>
        public int[] VibrationTwoVel { get; set; } = new int[2];

        /// <summary>
        /// 离心速度
        /// </summary>
        public int[] CentrifugalOneVelocity { get; set; } = new int[3];

        /// <summary>
        /// 离心时间
        /// </summary>
        public int[] CentrifugalOneTime { get; set; } = new int[3];

        /// <summary>
        /// 上清液提取量
        /// </summary>
        public double ExtractVolume { get; set; }

        public double cusuanan { get; set; }//醋酸铵加入量

        public double Extract { get; set; }   //完全倾倒


         /// <summary>
        /// 浓缩量
        /// </summary>
        public double ConcentrationVolume { get; set; }

        /// <summary>
        /// 浓缩速度
        /// </summary>
        public int ConcentrationVel { get; set; }

        /// <summary>
        /// 浓缩时间
        /// </summary>
        public int ConcentrationTime { get; set; }

        /// <summary>
        /// 定容复溶
        /// </summary>
        public double Redissolve { get; set; }

        /// <summary>
        /// 加标A
        /// </summary>
        public double Add_Mark_A { get; set; }
        
        /// <summary>
        /// 加标B
        /// </summary>
        public double Add_Mark_B { get; set; }



        /// <summary>
        /// 提取样品量
        /// </summary>
        public double ExtractSampleVolume { get; set; }


        /// <summary>
        /// 处理工艺（处理方法）
        /// GB23200.113-2018 果蔬    0xF43EE00
        /// GB23200.113-2018 坚果    0xF43EE19
        /// GB23200.121-2021 果蔬    0x803EEF9
        /// GB23200.121-2021 坚果    0x803EEF9
        /// 兽药                     0xFFDE1EB
        /// <see cref="TechStatus"/>
        /// </summary>
        public int Tech { get; set; }

        /// <summary>
        /// 工艺创建时间
        /// </summary>
        public DateTime Createtime { get; set; }
    }
}
