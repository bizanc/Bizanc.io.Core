using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IOfferRepository: IBaseRepository<Offer>
    {
        Task<bool> Contains(string hashStr);
        Task<bool> ContainsCancel(string hashStr);
        Task SaveCancel(OfferCancel obj);
        Task SaveCancel(IEnumerable<OfferCancel> list);
        Task<Offer> Get(string id);
        Task<List<Offer>> GetLast(int v);
        Task<IEnumerable<Offer>> GetByWallet(string wallet, int size);
    }
}