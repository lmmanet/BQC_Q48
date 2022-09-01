using BQJX.Common.Common;
using Q_Platform.Models;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface ICapperPosDataAccess
    {
        CapperPosData GetCapperPosData(int id);
        bool UpdateCapperPosData(CapperPosData data, int id);

        bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo);

        bool UpdatePosDataByAxisPosInfo(ushort id, List<AxisPosInfo> list);


    }
}