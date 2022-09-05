using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IVortex
    {
        Task<bool> StartVortex();
    }
}