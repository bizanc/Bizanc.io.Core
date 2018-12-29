using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IWalletRepository: IBaseRepository<Wallet>
    {
        Task<Wallet> Get();
    }
}