using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface ITransactionRepository: IBaseRepository<Transaction>
    {
        Task<bool> Contains(string hashStr);
        Task<List<Transaction>> GetLast(int size);

        Task<Transaction> Get(string txHash);

        Task<List<Transaction>> GetBySource(string wallet, int size);

        Task<List<Transaction>> GetByTarget(string wallet, int size);
    }
}