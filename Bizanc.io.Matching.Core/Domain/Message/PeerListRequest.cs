using System;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class PeerListRequest : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.PeerListRequest; } }
    }
}