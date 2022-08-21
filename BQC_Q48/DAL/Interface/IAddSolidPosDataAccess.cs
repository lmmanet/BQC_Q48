using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface IAddSolidPosDataAccess
    {
        AddSolidPosData GetPosData();
        bool UpdatePosData(AddSolidPosData data);
    }
}