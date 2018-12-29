using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class BlockResponse : BaseMessage
    {

        public override MessageType MessageType { get { return MessageType.BlockResponse; } }

        public virtual List<Block> Blocks { get; set; } = new List<Block>();

        public bool End { get; set; } = false;
    }
}