using System;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Infra.Repository;
using Bizanc.io.Matching.Core.Domain;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Oracle.Repository
{
    public class WithdrawInfoRepository : BaseRepository<WithdrawInfo>
    {
        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<WithdrawInfo>().Where(b => b.HashStr == hashStr).AnyAsync();
        }
    }
}