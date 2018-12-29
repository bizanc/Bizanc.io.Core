using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class OpStats
    {
        public int Total { get; set; }
        public double AvgBlock { get; set; }

        public double AvgSecond { get; set; }
        public int Pool { get; set; }
    }
}