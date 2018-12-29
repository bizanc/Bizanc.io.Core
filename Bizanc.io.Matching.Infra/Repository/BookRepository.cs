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
    public class BookRepository : BaseRepository<Book>, IBookRepository
    {
        public async Task<Book> Get(string blockHash)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Book>().Where(b => b.BlockHash == blockHash)
                .FirstOrDefaultAsync();
        }

        public async Task Delete(string blockHash)
        {
            using (var s = Store.OpenAsyncSession())
            {
                string id = await s.Query<Book>().Where(b => b.BlockHash == blockHash).Select(bl => bl.Id).FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(id))
                {
                    s.Delete(id);
                    await s.SaveChangesAsync();
                }
            }

            CleanIndexes();
        }

        private async void CleanIndexes()
        {
            IndexDefinition index = await Store.Maintenance.SendAsync(new GetIndexOperation("Auto/Books/ByBlockHash"));

            if (index != null)
                await Store.Maintenance.SendAsync(new DisableIndexOperation("Auto/Books/ByBlockHash"));
        }
    }
}