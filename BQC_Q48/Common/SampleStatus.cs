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


        IsPurfyInShelf = 10,              //净化管在试管架
        IsPurfyInCapper = 11,         //净化管在拧盖3
        IsPurfyUnCapped = 12,             //净化管已拆盖
        IsPurfyInVibration = 13,        //净化管在振荡2
        IsPurfyInTransfer = 14,           //净化管在移栽
        IsPurfyInCentrifugal = 15,        //净化管在离心机


        IsSelingInShelf = 16,              //西林瓶在试管架
        IsSelingInCapper = 17,          //西林瓶在拧盖4
        IsSelingUnCapped = 18,             //西林瓶已拆盖
        IsSelingInConcentration = 19,        //西林瓶在浓缩
        IsSelingInWeigh = 20,                //西林瓶在浓缩称重

        IsBottleInShelf = 21,         //进样小瓶在试管架
        IsBottleInCapper = 22,           //进样小瓶在拧盖5
        IsBottleUnCapped = 23,              //进样小瓶已拆盖

        IsPolishInShelf =24,               //兽药样品萃取试管
        IsPolishInCapper = 25,             //兽药样品萃取试管
        IsPolishUnCapped = 26,             //兽药样品萃取试管
        IsPolishInVibration = 27,          //兽药样品萃取试管
        IsPolishInVortexed = 28,           //兽药样品萃取试管
        IsPolishInCold = 29,               //兽药样品萃取试管
        IsPolishInTransfer = 30,         //兽药样品萃取试管
        IsPolishInCentrifugal = 31       //兽药样品萃取试管

    }


  





}
