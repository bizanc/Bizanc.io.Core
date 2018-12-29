using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IPeerManager
    {
        void Connect(IPeer peer);

        Task Disconnect(IPeer peer);
    }
}