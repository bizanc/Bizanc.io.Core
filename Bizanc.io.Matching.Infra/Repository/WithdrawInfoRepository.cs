using System;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Infra.Repository;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class WithdrawInfoRepository : BaseRepository<WithdrawInfo>, IWithdrawInfoRepository
    {
        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<WithdrawInfo>().Where(b => b.HashStr == hashStr).AnyAsync();
        }

        public async Task<WithdrawInfo> Get(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<WithdrawInfo>().Where(b => b.HashStr == hashStr).FirstOrDefaultAsync();
        }

        public async Task<string> GetLastEthBlockNumber()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<WithdrawInfo>().Where(d => d.Asset != "BTC").OrderByDescending(d => d.Timestamp).Select(d => d.BlockNumber);
                return await dp.FirstOrDefaultAsync();
            }
        }

        public async Task<string> GetLastBtcBlockNumber()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<WithdrawInfo>().Where(d => d.Asset == "BTC").OrderByDescending(d => d.Timestamp).Select(d => d.BlockNumber);
                return await dp.FirstOrDefaultAsync();
            }
        }
    }
}