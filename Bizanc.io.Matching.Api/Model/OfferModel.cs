using System;
using System.Collections.Generic;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Api.Model
{
    public class OfferModel
    {
        public string Wallet { get; set; }
        public string Asset { get; set; }
        public OfferType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public long Timestamp { get; set; }
        public string Signature { get; set; }
    }
}