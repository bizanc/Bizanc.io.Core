using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class WithdrawInfo
    {
        public string HashStr { get; set; }

        public string TxHash { get; set; }

        public DateTime Timestamp { get; set; }
    }
}