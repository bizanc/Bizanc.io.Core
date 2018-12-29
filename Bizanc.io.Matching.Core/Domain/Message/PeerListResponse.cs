using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class PeerListResponse : BaseMessage
    {

        public override MessageType MessageType { get { return MessageType.PeerListResponse; } }

        public virtual IList<string> Peers { get; set; }
    }
}