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
    public class TradeRepository : BaseRepository<Trade>, ITradeRepository
    {
        public ChannelReader<Trade> ListAscending(string asset, DateTime from)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var query = s.Query<Trade>().Where(t => t.Asset == asset && t.Timestamp >= from).OrderBy(t => t.Timestamp);
                var channel = Channel.CreateUnbounded<Trade>();
                Stream(s, query, channel);
                return channel.Reader;
            }
        }

        public async Task<List<Trade>> ListDescending(string asset, DateTime from, int limit)
        {
            var result = new List<Trade>();
            using (var s = Store.OpenAsyncSession())
            {
                var query = s.Query<Trade>().Where(t => t.Asset == asset && t.Timestamp >= from).Take(limit).OrderByDescending(t => t.Timestamp);

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