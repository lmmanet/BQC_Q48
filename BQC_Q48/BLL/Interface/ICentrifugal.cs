using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface ICentrifugal
    {
        Task<bool> Centri_GoHome();
        void CloseShadow();
        Task<bool> C_GoHome();
        Task<bool> GoHome();
        void GoStation(ushort num);
        void OpenShadow();
        Task<bool> X_GoHome();
        void Y_Cylinder_Get();
        void Y_Cylinder_Put();
        Task<bool> Z_GoHome();
    }
}