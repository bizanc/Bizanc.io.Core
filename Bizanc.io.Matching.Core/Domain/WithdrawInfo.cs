using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class WithdrawInfo
    {
        public string Id { get; set; }
        public string Asset { get; set; }
        public string HashStr { get; set; }
        public string TxHash { get; set; }
        public string BlockNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}