using System.Threading;
using System.Threading.Tasks;

namespace Q_Platform.BLL
{
    public interface IConcentration
    {
        Task<bool> GoHome(CancellationTokenSource cts);




    }
}