using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IAddSolid
    {
        Task<bool> AddSolidAsync(int solid, double weight, CancellationTokenSource cts);
        Task<bool> X_Left();
        Task<bool> X_Right();
        Task<bool> Z1_High();
        Task<bool> Z1_Low();
        Task<bool> Z2_High();
        Task<bool> Z2_Low();
    }
}