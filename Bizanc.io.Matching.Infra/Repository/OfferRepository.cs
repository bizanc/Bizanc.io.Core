using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Repository;
using Raven.Client.Documents;

namespace Bizanc.io.Matching.Infra.Repository
{
    public class OfferRepository : BaseRepository<Offer>, IOfferRepository
    {
        public OfferRepository(string db = null)
        : base(db)
        { }

        public async Task<bool> Contains(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Offer>().Where(b => b.HashStr == hashStr).AnyAsync();
        }

        public async Task<bool> ContainsCancel(string hashStr)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<OfferCancel>().Where(b => b.HashStr == hashStr).AnyAsync();
        }

        public async Task SaveCancel(OfferCancel obj)
        {
            using (var s = Store.OpenAsyncSession())
            {
                await s.StoreAsync(obj);
                await s.SaveChangesAsync();

                s.Advanced.WaitForIndexesAfterSaveChanges();
            }
        }

        public async Task SaveCancel(IEnumerable<OfferCancel> list)
        {
            using (var s = Store.OpenAsyncSession())
            {
                foreach (var obj in list)
                    await s.StoreAsync(obj);

                await s.SaveChangesAsync();

                s.Advanced.WaitForIndexesAfterSaveChanges();
            }
        }

        public async Task<List<Offer>> GetLast(int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var of = s.Query<Offer>().OrderByDescending(d => d.Timestamp).Take(size);
                return await of.ToListAsync();
            }
        }

        public async Task<Offer> Get(string id)
        {
            using (var s = Store.OpenAsyncSession())
                return await s.Query<Offer>().Where(d => d.HashStr == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Offer>> GetByWallet(string wallet, int size)
        {
            using (var s = Store.OpenAsyncSession())
            {
                var dp = s.Query<Offer>().
                            Where(d => d.Wallet == wallet)
                            .OrderByDescending(d => d.Timestamp)
                            .Take(size);
                return await dp.ToListAsync();
            }
        }
    }
}