using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Communication.RuiTe
{
    public enum EnumStatus
    {
        ENA = 1,      //驱动器使能标志
        ALM = 2,      //驱动器报警标志
        InPos = 4,    //闭环模式时电机定位完成标志
        Mov = 8,      //电机运动标志
        Home = 16,    //回零标志
        Rdy = 32,     //驱动器准备就绪标志
        ArrSpd = 64,  //电机是否运行到设定速度
        Clamp = 128,  //电机机械抱闸状态
        PL = 256,     //正限位有效状态
        NL = 512,     //负限位有效状态
        Pow = 1024,   //电源状态
        TC = 2048,    //力矩到达状态
    }

    public enum EnumAlm
    {
        IVE = 1,       //内部电压错误报警标志
        OC = 2,        //过流报警标志
        OV = 4,        //过压报警标志
        UV = 8,        //欠压报警标志
        OT = 16,       //过温报警标志
        MEM = 32,      //参数校验错误
        MPE = 64,      //电机缺相报警
        POSE = 128,    //跟踪误差报警
        ECDE1 = 256,   // 编码器故障
    }
}
