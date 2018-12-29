using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IBlockRepository: IBaseRepository<Block>
    {
        Task<bool> Contains(string hashStr);
        
        Task<Block> Get(string blockHash);

        Task<Channel<Block>> Get(long fromDepth, long toDepth = 0);

        Task SavePersistInfo(BlockPersistInfo info);

        Task DeletePersistInfo(string blockHash);

        Task<BlockPersistInfo> GetPersistInfo();

        Task<Stats> GetBlockStats();
    }
}