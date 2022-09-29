using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public enum SampleStatus
    {
        IsInShelf = 0,                //50ml离心管在试管架
        IsInCapperOne = 1,            //50ml离心管在拧盖1
        IsUnCapped = 2,               //50ml离心管已拆盖
        IsInAddSolid = 3,             //50ml离心管在加固
        IsInVortexed = 4,             //50ml离心管在涡旋
        IsInCapperTwo = 5,            //50ml离心管在拧盖2
        IsInVibrationOne = 6,         //50ml离心管在振荡1
        IsInCold = 7,                 //50ml离心管在冰浴
        IsInTransfer = 8,             //50ml离心管在移栽
        IsInCentrifugal = 9,          //50ml离心管在离心


        IsPurfyInShelf = 12,              //净化管在试管架
        IsPurfyInCapper = 13,         //净化管在拧盖3
        IsPurfyUnCapped = 14,             //净化管已拆盖
        IsPurfyInVibration = 15,        //净化管在振荡2
        IsPurfyInTransfer = 16,           //净化管在移栽
        IsPurfyInCentrifugal = 17,        //净化管在离心机


        IsSelingInShelf = 20,              //西林瓶在试管架
        IsSelingInCapper = 21,          //西林瓶在拧盖4
        IsSelingUnCapped = 22,             //西林瓶已拆盖
        IsSelingInConcentration = 23,        //西林瓶在浓缩
        IsSelingInWeigh = 24,                //西林瓶在浓缩称重

        IsBottle1InShelf = 32,            //进样小瓶1在试管架
        IsBottle1InCapper = 33,           //进样小瓶1在拧盖5
        IsBottle1UnCapped = 34,           //进样小瓶1已拆盖
        IsBottle1ExtractDone =35,         //进样小瓶1完成取样
        IsBottle2InShelf = 36,            //进样小瓶2在试管架
        IsBottle2InCapper = 37,           //进样小瓶2在拧盖5
        IsBottle2UnCapped = 38,           //进样小瓶2已拆盖
        IsBottle2ExtractDone = 39,        //进样小瓶2完成取样



        IsPolishInShelf =40,               //兽药样品萃取试管
        IsPolishInCapper = 41,             //兽药样品萃取试管
        IsPolishUnCapped = 52,             //兽药样品萃取试管
        IsPolishInVibration = 53,          //兽药样品萃取试管
        IsPolishInVortexed = 44,           //兽药样品萃取试管
        IsPolishInCold = 45,               //兽药样品萃取试管
        IsPolishInTransfer = 46,         //兽药样品萃取试管
        IsPolishInCentrifugal = 47       //兽药样品萃取试管

    }


  





}
