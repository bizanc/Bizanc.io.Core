using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Stats
    {
        public BlockStats Blocks { get; set; } = new BlockStats();

        public OpStats Deposits { get; set; } = new OpStats();

        public OpStats Offers { get; set; } = new OpStats();

        public OpStats Transactions { get; set; } = new OpStats();

        public OpStats Withdrawals { get; set; } = new OpStats();
    }
}