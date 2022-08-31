using BQJX.Common.Common;
using Q_Platform.Models;
using System.Collections.Generic;

namespace Q_Platform.DAL
{
    public interface IAddSolidPosDataAccess
    {
        AddSolidPosData GetPosData();
        bool UpdatePosData(AddSolidPosData data);

        bool UpdatePosDataByAxisPosInfo(ushort id, AxisPosInfo posInfo);

        bool UpdatePosDataByAxisPosInfo(ushort id, List<AxisPosInfo> list);
    }
}