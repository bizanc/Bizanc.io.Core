using System;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class TransactionPoolRequest : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.TransactionPoolRequest; } }
    }
}