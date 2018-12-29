using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Asset
    {
        public string Id { get; set; }
        public decimal LastPrice { get; set; }
        public decimal BestBid { get; set; }
        public decimal BestAsk { get; set; }
    }
}