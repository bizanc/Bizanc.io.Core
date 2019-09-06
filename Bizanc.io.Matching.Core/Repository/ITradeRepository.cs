using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface ITradeRepository : IBaseRepository<Trade>
    {
        ChannelReader<Trade> ListAscending(string asset, DateTime from);

        Task<List<Trade>> ListDescending(string asset, DateTime from, int limit);
    }
}