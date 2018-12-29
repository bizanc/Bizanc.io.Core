using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bizanc.io.Matching.Core.Domain.Messages;
using System.Globalization;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Candle
    {
        public DateTime Date { get; set; }

        public virtual decimal Open { get; set; } = 0;

        public virtual decimal High { get; set; } = 0;

        public virtual decimal Low { get; set; } = 0;

        public virtual decimal Close { get; set; } = 0;

        public decimal Volume{ get; set; }

        public Candle Clone()
        {
            return (Candle)MemberwiseClone();
        }
    }

    public enum CandlePeriod
    {
        minute_1 = 1,
        minute_5 = 5,
        minute_15 = 15,
        minute_30 = 30,
        hour_1 = 60,
        hour_12 = 60 * 12,
        day_1 = 60 * 24,
        week_1 = CandlePeriod.day_1 * 7
    }
}