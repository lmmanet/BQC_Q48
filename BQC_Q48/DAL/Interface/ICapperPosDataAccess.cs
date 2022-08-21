using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface ICapperPosDataAccess
    {
        CapperPosData GetCapperPosData(int id);
        bool UpdateCapperPosData(CapperPosData data, int id);
    }
}