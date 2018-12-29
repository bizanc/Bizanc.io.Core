using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class WalletRepository : BaseRepository<Wallet>, IWalletRepository
    {
        public async Task<Wallet> Get()
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Wallet>().FirstOrDefaultAsync();
        }
    }
}