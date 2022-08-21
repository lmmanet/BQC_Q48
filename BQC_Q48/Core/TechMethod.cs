using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q_Platform.Core
{
    public enum TechMethod
    {
        AddSolvent_First = 0,            //一次加液   bit0
        Vortex = 1,                      //涡旋
        WetBack = 2,                     //回湿
        AddSolvent_Second = 3,           //二次加液
        VibrationOne = 4,                //提取振荡
        AddSolid =5,                     //加固
        VibrationTwo = 6,                //振荡混匀
        ExtractCentrifugal = 7,          //离心
        ExtractPiperttor =8,             //提取上清液
        VibrationThree = 9,              //振荡混匀
        PurifyCentrifugal = 10,          //离心
        Concentration = 11,              //浓缩
        AddStandan = 12 ,                //加标
        DingrongFurong = 13,             //定容复溶
        ExtractSample = 14               //提取样品
        
    }
}
