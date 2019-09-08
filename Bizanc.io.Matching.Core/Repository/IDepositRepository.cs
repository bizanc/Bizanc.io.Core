using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IDepositRepository: IBaseRepository<Deposit>
    {
        Task<bool> Contains(string hashStr);

        Task<List<Deposit>> GetLast(int size);

        Task<string> GetLastEthBlockNumber();

        Task<string> GetLastBtcBlockNumber();

        Task<Deposit> Get(string txHash);

        Task<Deposit> GetByTxHash(string txHash);

        Task<List<Deposit>> GetByTarget(string wallet, int size);

        Task<ChannelReader<Deposit>> List();

        Task<ChannelReader<Deposit>> List(DateTime from);
    }
}