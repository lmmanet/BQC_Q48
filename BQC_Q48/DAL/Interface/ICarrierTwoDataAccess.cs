using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface ICarrierTwoDataAccess
    {
        CarrierTwoPosData GetPosData();
        bool UpdatePosData(CarrierTwoPosData data);
    }
}