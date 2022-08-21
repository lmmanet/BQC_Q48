using BQJX.Common.Common;

namespace Q_Platform.DAL
{
    public interface ICentrifugalCarrierPosDataAccess
    {
        CentrifugalCarrierPosData GetPosData();
        bool UpdatePosData(CentrifugalCarrierPosData data);
    }
}