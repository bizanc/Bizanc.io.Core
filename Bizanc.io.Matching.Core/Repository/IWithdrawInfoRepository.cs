using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IWithdrawInfoRepository: IBaseRepository<WithdrawInfo>
    {
        Task<bool> Contains(string hashStr);

        Task<WithdrawInfo> Get(string hashStr);

        Task<string> GetLastEthBlockNumber();

        Task<string> GetLastBtcBlockNumber();
    }
}