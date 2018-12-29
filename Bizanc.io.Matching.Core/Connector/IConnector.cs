using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Util;

namespace Bizanc.io.Matching.Core.Connector
{
    public interface IConnector
    {
        ChannelReader<Deposit> GetChannelReader();

        Task<IEnumerable<Deposit>> Start();
    }
}