using System;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Deposit : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.Deposit; } }

        public string TargetWallet { get; set; }

        public string Asset { get; set; }

        public string AssetId { get; set; }

        public decimal Quantity { get; set; }

        public string TxHash { get; set; }

        public string BlockNumber { get; set; }

        public Deposit Clone() =>
            Clone<Deposit>();

        public override string ToString() =>
            TargetWallet + Asset + AssetId + Quantity.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US")) + TxHash + BlockNumber;
    }
}