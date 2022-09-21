using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common
{
    public enum SampleStatus
    {
        IsInShelf = 0,                //样品在试管架
        IsInCapperOne = 1,            //样品在拧盖1
        IsUnCapped = 2,               //样品已拆盖
        IsInAddSolid = 3,             //样品在加固
        IsInVortexed = 4,             //样品在涡旋
        IsInCapperTwo = 5,            //样品在拧盖2
        IsInVibrationOne = 6,         //样品在振荡1
        IsInCold = 7,                 //样品在冰浴
        IsInTransfer = 8,             //样品在移栽

        IsInCentrifugal = 9,          //样品在离心
        IsInShelf2 = 10,              //样品在净化试管架
        IsInCapperThree = 11,         //样品在拧盖3
        IsInVibrationTwo = 12,        //样品在振荡2
        IsInShelf3 = 13,              //样品在西林瓶试管架
        IsInCapperFour = 14,          //样品在拧盖4
        IsInConcentration =15,        //样品在浓缩
        IsInWeigh =16,                //样品在浓缩称重
        IsInCapperFive =17,           //样品在拧盖5
        IsInBottleShelf =18           //样品在小瓶架  完成

    }


  





}
