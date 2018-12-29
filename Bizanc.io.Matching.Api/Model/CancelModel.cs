using System;
using System.Collections.Generic;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Api.Model
{
    public class OfferCancelModel
    {
        public string Offer { get; set; }
        public string Wallet { get; set; }
        public long Timestamp { get; set; }
        public string Signature { get; set; }
    }
}