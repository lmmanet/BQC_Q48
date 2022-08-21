using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface ICarrierOneDataAccess
    {
        CarrierOnePosData GetPosData();
        bool UpdatePosData(CarrierOnePosData data);
    }
}