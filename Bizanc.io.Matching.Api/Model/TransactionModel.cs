using System;
using System.Collections.Generic;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Api.Model
{
    public class TransactionModel
    {
        public string SourceWallet { get; set; }
        public string TargetWallet { get; set; }
        public string Asset { get; set; }
        public decimal Size { get; set; }
        public long Timestamp { get; set; }
        public string Signature { get; set; }
    }
}