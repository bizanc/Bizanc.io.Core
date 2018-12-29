using System;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class HandShake : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.HandShake; } }

        public string AppVersion { get; set; }

        public long BlockLength { get; set; }

        public string Address { get; set; }

        public int ListenPort { get; set; }
    }
}