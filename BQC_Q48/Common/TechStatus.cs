using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public enum TechStatus
    {                            
        AddWater = 0,            //样品加水（加液）
        AddHomo = 1,             //样品加均质子（加固）
        Vibration = 2,           //样品加均质子（振荡）
        Vortex = 3,              //样品涡旋（涡旋）

        WetBack = 4,             //样品回湿

        AddSolve1 = 5,           //样品加溶剂（加液）
        AddSalt1 = 6,            //样品加盐或加均质子（加固）
        ExtractVibration1 = 7,   //样品振荡提取
        ExtractVortex1 = 8,      //样品涡旋提取

        AddSolve2 = 9,           //样品加盐（加液）
        AddSalt2 = 10,           //样品加盐或加均质子（加固）
        ExtractVibration2 = 11,  //样品振荡提取（盐析）
        ExtractVortex2 = 12,     //样品涡旋提取

        Centrifugal1 = 13,        //样品一次离心
        ExtractSupernate = 14,    //样品取上清液
        PurifyVibration = 15,     //样品净化振荡
        Centrifugal2 = 16,        //样品二次离心          
        ExtractPurify = 17,       //样品提取净化液

        ExtractSupernate2 = 18,   //样品三次提取（兽药）
        ExtractVibration3 = 19,   //样品三次振荡（兽药）
        ExtractVortex3 = 20,      //样品三次涡旋（兽药）
        Centrifugal3 = 21,        //样品三次离心（兽药）

        ExtractSupernate3 = 22,   //样品移取上清液浓缩（兽药）

        AddMark1 = 23,            //样品加标1
        NitrogenBlow = 24,        //样品氮吹
        AddMark2 = 25,            //样品加标2
        Redissolve = 26,          //样品复溶
        ExtractSample = 27,       //样品取样

    }
}





