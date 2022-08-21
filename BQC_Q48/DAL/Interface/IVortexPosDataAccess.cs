using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface IVortexPosDataAccess
    {
        VortexPosData GetPosData();
        bool UpdatePosData(VortexPosData data);
    }
}