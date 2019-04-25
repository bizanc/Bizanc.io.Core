using System;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IPeerListener
    {
        Task Start();

        Task<IPeer> Connect(string address);

        Task<IPeer> Accept();
    }
}