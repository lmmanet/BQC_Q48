using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface IConcentrationPosDataAccess
    {
        ConcentrationPosData GetPosData();
        bool UpdatePosData(ConcentrationPosData data);
    }
}