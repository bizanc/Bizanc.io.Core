using System;
using System.Collections.Generic;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Transaction : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.Transaction; } }


        public List<TransactionOutput> Outputs { get; set; } = new List<TransactionOutput>();

        public string Asset { get; set; }

        public string Wallet { get; set; }

        public string Signature;

        public override string ToString()
        {
            var result = Wallet + Asset + TimeStampTicks;

            foreach (var o in Outputs)
                result += o.Wallet + o.Size.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US"));

            return result;
        }

        public Transaction Clone()
        {
            return Clone<Transaction>();
        }

        
    }
}