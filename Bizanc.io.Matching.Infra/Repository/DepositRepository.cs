using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class DepositRepository : BaseRepository<Deposit>, IDepositRepository
    {
        public DepositRepository(string db = null)
        : base(db)
        { }

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

        public async Task<string> GetLastEthBlockNumber()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<Deposit>().Where(d => d.Asset != "BTC").OrderByDescending(d => d.Timestamp).Select(d => d.BlockNumber);
                return await dp.FirstOrDefaultAsync();
            }
        }

        public async Task<string> GetLastBtcBlockNumber()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<Deposit>().Where(d => d.Asset == "BTC").OrderByDescending(d => d.Timestamp).Select(d => d.BlockNumber);
                return await dp.FirstOrDefaultAsync();
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

        public async Task<ChannelReader<Deposit>> List()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var result = Channel.CreateUnbounded<Deposit>(); ;
                await StreamResult(s, s.Query<Deposit>(), result);

                return result;
            }
        }

        public async Task<ChannelReader<Deposit>> List(DateTime from)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var result = Channel.CreateUnbounded<Deposit>(); ;
                await StreamResult(s, s.Query<Deposit>().Where(d => d.Timestamp >= from), result);

                return result;
            }
        }
    }
}