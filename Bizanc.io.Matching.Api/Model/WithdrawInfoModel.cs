using System;
using System.Collections.Generic;
using System.Globalization;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Api.Model
{
    public class WithdrawInfoModel : WithdrawalModel
    {
        public string HashStr { get; set; }
        public string TxHash { get; set; }
        public bool Mined { get; set; }
        public virtual DateTime FormattedTimestamp
        {
            get { return new DateTime(Timestamp, DateTimeKind.Utc); }
        }
    }
}