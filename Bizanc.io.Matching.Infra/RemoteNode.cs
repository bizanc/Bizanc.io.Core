using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Infra
{
    // public class RemoteNode : IPeer
    // {
    //     private IPeer host;
    //     private Node remote;

    //     public RemoteNode(IPeer host, Node remote)
    //     {
    //         this.host = host;
    //         this.remote = remote;
    //     }

    //     public Task SendMessage(BaseMessage message)
    //     {
    //         throw new NotImplementedException();
    //     }

    //     async Task IPeer.GetBlocks()
    //     {
    //        await remote.GetBlocks(host);
    //     }

    //     async Task IPeer.Message(Offer offer)
    //     {
    //         await remote.Message(offer);
    //     }

    //     async Task IPeer.Message(Block block)
    //     {
    //         await remote.Message(block);
    //     }
    // }

}