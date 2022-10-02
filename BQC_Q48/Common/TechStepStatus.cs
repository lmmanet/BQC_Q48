using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public enum TechStepStatus
    {
        AddWaterExt =1,           //加水提取
        //AddHomo = 1,     子步骤
        //搬运   = 2
        //Vibration = 3,   子步骤
        //Vortex = 4,      子步骤
        AddSolveExt =2,           //加溶剂提取
        //AddHomo = 1,     子步骤
        //Vibration = 3,   子步骤
        //Vortex = 4,      子步骤
        AddSolidExt = 3,           //加盐包提取
        //AddHomo = 1,     子步骤
        //Vibration = 2,   子步骤
        //Vortex = 3,      子步骤
                                            
        CentrifugalOne = 4,     //一次离心

        Pipettor = 5,             //取上清液

        VibrationTwo =6,          //净化振荡

        CentrifugalTwo = 7,       //二次离心

        Supernate =8,             //提取净化液

        VibrationAndVortex =9,     //振荡涡旋  兽药

        CentrifugalThree = 10,          //三次离心

        Supernate2 = 11,          //提取浓缩液

        AddMark1 =12,          //净化振荡

        Concentration =13,          //浓缩

        Redissolve = 14,          //复溶

        GetSample =15,          //取样品液

    }
}
