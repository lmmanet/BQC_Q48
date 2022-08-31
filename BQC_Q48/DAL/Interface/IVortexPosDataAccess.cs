using BQJX.Common.Common;
using Q_Platform.Models;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface IVortexPosDataAccess
    {
        VortexPosData GetPosData();
        bool UpdatePosData(VortexPosData data);
        bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo);

        bool UpdatePosDataByAxisPosInfo(ushort id, List<AxisPosInfo> list);
    }
}