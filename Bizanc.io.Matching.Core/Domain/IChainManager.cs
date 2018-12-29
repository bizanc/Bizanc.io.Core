using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IChainManager
    {
        void ProcessMinedBlock(Chain sender, Block block);
    }
}