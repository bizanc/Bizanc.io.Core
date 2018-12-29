using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class DepositRepository : BaseRepository<Deposit>, IDepositRepository
    {
        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Deposit>().Where(b => b.HashStr == hashStr).AnyAsync();
        }

        public async Task<List<Deposit>> GetLast(int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<Deposit>().OrderByDescending(d => d.Timestamp).Take(size);
                return await dp.ToListAsync();
            }
        }

        public async Task<Deposit> Get(string dpHash)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Deposit>().Where(d => d.HashStr == dpHash).FirstOrDefaultAsync();
        }

        public async Task<Deposit> GetByTxHash(string txHash)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Deposit>().Where(d => d.TxHash == txHash).FirstOrDefaultAsync();
        }

        public async Task<List<Deposit>> GetByTarget(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Deposit>().
                            Where(d => d.TargetWallet == wallet)
                            .OrderByDescending(d => d.Timestamp)
                            .Take(size)
                            .ToListAsync();
        }
    }
}