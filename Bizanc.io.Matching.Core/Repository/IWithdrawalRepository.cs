using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IWithdrawalRepository: IBaseRepository<Withdrawal>
    {
        Task<bool> Contains(string hashStr);

        Task<List<Withdrawal>> GetLast(int size);

        Task<Withdrawal> Get(string txHash);

        Task<List<Withdrawal>> GetBySource(string wallet, int size);

        Task<List<Withdrawal>> GetByTarget(string wallet, int size);
    }
}