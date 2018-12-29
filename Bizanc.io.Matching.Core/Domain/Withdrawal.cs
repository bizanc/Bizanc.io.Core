using System;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Withdrawal : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.Withdrawal; } }

        public string SourceWallet { get; set; }
        
        public string TargetWallet { get; set; }

        public string Asset { get; set; }

        public decimal Size { get; set; }

        public string Signature;

        public override string ToString()
        {
            return SourceWallet + TargetWallet + Asset + TimeStampTicks + Size.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US"));
        }
    }
}