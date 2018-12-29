using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class TransactionPoolResponse : BaseMessage
    {

        public override MessageType MessageType { get { return MessageType.TransactionPoolResponse; } }

        public virtual List<Transaction> TransactionPool { get; set; }
    }
}