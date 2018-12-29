using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bizanc.io.Matching.Core.Domain.Messages;
using System.Globalization;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Core.Domain
{
    public class OfferCancel : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.OfferCancel; } }
        public string Offer { get; set; }
        public string Wallet { get; set; }
        public string Signature;

        public override string ToString()
        {
            return Offer + Wallet + TimeStampTicks;
        }

        public OfferCancel Clone()
        {
            return Clone<OfferCancel>();
        }
    }
}