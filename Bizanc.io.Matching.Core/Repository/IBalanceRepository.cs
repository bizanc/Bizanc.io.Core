using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Immutable;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IBalanceRepository:IBaseRepository<Balance>
    {
        Task<Balance> Get(string blockHash);

        Task Delete(string blockHash);
    }
}