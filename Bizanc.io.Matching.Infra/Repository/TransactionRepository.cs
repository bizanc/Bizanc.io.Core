using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Transaction>().Where(b => b.HashStr == hashStr).AnyAsync();
        }

        public async Task<List<Transaction>> GetLast(int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var tx = s.Query<Transaction>().OrderByDescending(t => t.Timestamp).Take(size);
                return await tx.ToListAsync();
            }
        }

        public async Task<Transaction> Get(string txHash)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Transaction>().Where(b => b.HashStr == txHash).FirstOrDefaultAsync();
        }

        public async Task<List<Transaction>> GetBySource(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var tx = s.Query<Transaction>().
                            Where(t => t.Wallet == wallet)
                            .OrderByDescending(t => t.Timestamp)
                            .Take(size);
                return await tx.ToListAsync();
            }
        }

        public async Task<List<Transaction>> GetByTarget(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var tx = s.Query<Transaction>().
                            Where(t => t.Outputs.Any(o => o.Wallet == wallet))
                            .OrderByDescending(t => t.Timestamp)
                            .Take(size);
                return await tx.ToListAsync();
            }
        }
    }
}