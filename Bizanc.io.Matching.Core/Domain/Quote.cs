using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Quote
    {
        public string Asset{ get; set; }
        public decimal LastPrice{ get; set; }
        public decimal Open { get; set; } = 0;

        public decimal High { get; set; } = 0;

        public decimal Low { get; set; } = 0;

        public decimal Volume { get; set; } = 0;
    }
}