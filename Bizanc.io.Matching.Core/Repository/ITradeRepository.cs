using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface ITradeRepository : IBaseRepository<Trade>
    {
        Task<List<Trade>> List(string asset, DateTime from);
    }
}