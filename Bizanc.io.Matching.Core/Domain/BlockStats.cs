using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class BlockStats
    {
        public DateTime FirstBlockTime { get; set; }
        public DateTime LastBlockTime { get; set; }
        public int TotalCount { get; set; }
        public int TotalOpCount { get; set; }
        
        public double AvgBlockTime
        {
            get
            {
                return ((double)(LastBlockTime - FirstBlockTime).TotalSeconds / TotalCount);
            }
        }

        public double SecondsSinceLastBlock
        {
            get
            {
                return (DateTime.Now.ToUniversalTime() - LastBlockTime).TotalSeconds;
            }
        }

        public double AvgOpCount
        {
            get
            {
                return ((double)TotalOpCount / TotalCount);
            }
        }
    }
}