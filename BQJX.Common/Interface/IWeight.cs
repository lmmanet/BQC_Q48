using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQJX.Common.Interface
{
    public interface IWeight
    {
        Task<bool> Clear(ushort id);
        Task<bool> ReadIsStatic(ushort id);
        Task<double> ReadWeight(ushort id);
        Task<int> ReadStatus(ushort id);
    }
}
