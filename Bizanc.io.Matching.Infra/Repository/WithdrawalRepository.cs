using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class WithdrawalRepository : BaseRepository<Withdrawal>, IWithdrawalRepository
    {
        public WithdrawalRepository(string db = null)
        : base(db)
        { }

        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Withdrawal>().Where(w => w.HashStr == hashStr).AnyAsync();
        }

        public async Task<List<Withdrawal>> GetLast(int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var wd = s.Query<Withdrawal>().OrderByDescending(w => w.Timestamp).Take(size);
                return await wd.ToListAsync();
            }
        }

        public async Task<Withdrawal> Get(string txHash)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Withdrawal>().Where(w => w.HashStr == txHash).FirstOrDefaultAsync();
        }

        public async Task<List<Withdrawal>> GetBySource(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var wd = s.Query<Withdrawal>()
                            .Where(w => w.SourceWallet == wallet)
                            .OrderByDescending(w => w.Timestamp)
                            .Take(size);
                return await wd.ToListAsync();
            }
        }

        public async Task<List<Withdrawal>> GetByTarget(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var wd = s.Query<Withdrawal>().
                            Where(w => w.TargetWallet == wallet)
                            .OrderByDescending(w => w.Timestamp)
                            .Take(size);
                return await wd.ToListAsync();
            }
        }
    }
}
