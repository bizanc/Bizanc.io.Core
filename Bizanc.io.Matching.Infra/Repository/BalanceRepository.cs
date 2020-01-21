using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Immutable;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Operations.Indexes;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class BalanceRepository : BaseRepository<Balance>, IBalanceRepository
    {
        public BalanceRepository(string db = null)
        : base(db)
        { }

        public async Task<List<Balance>> Get()
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Balance>().ToListAsync();
        }

        public async Task Clean()
        {
            using (var s = Store.OpenAsyncSession())
            {
                var points = await s.Query<Balance>().Select(bl => new { id = bl.Id, timestap = bl.Timestamp }).ToListAsync();
                if (points.Count > 20)
                {
                    points = points.OrderBy(p => p.timestap).ToList();
                    while (points.Count > 20)
                    {
                        var id = points[0].id;
                        if (!string.IsNullOrEmpty(id))
                        {
                            s.Delete(id);
                            await s.SaveChangesAsync();
                        }
                        points.RemoveAt(0);
                    }
                }
            }
        }
    }
}