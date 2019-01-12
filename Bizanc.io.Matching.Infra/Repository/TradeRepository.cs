using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class TradeRepository : BaseRepository<Trade>, ITradeRepository
    {
        public async Task<List<Trade>> List(string asset, DateTime from)
        {
            var result = new List<Trade>();
            using (var s = Store.OpenAsyncSession())
            {
                var query = s.Query<Trade>().Where(t => t.Asset == asset && t.Timestamp >= from).OrderByDescending(t => t.Timestamp);
                
                using (var stream = await s.Advanced.StreamAsync(query))
                {
                    while (await stream.MoveNextAsync())
                        result.Add(stream.Current.Document);
                }
            }

            return result;
        }

    }
}