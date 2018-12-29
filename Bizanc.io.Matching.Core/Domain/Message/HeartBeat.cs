using System;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class HeartBeat : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.HeartBeat; } }
    }
}