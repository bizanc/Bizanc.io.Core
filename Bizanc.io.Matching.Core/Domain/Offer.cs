using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bizanc.io.Matching.Core.Domain.Messages;
using System.Globalization;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Offer : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.Offer; } }
        public List<Trade> Trades { get; set; } = new List<Trade>();
        public OfferType Type { get; set; }

        public string Wallet { get; set; }

        public string Asset { get; set; }

        public virtual decimal Price { get; set; } = 0;

        public virtual decimal Quantity { get; set; } = 0;

        public decimal LeavesQuantity { get { return Quantity - ExecQuantity; } }
        public virtual decimal ExecQuantity { get { return Trades.Sum(t => t.Quantity); } }

        public virtual OfferStatus Status { get; set; }

        public string Signature;

        public override string ToString()
        {
            return Wallet + Asset + (int)Type + TimeStampTicks + Quantity.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US")) + Price.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US"));
        }

        public void AddTrade(Trade trade)
        {
            Trades.Add(trade);
        }

        public override T Clone<T>()
        {
            var result = (Offer)MemberwiseClone();

            result.Trades = new List<Trade>();
            result.Trades = Trades.Select(t => t.Clone()).ToList();

            return (T)((BaseMessage)result);
        }

        public Offer Convert(decimal price)
        {
            var result = (Offer)MemberwiseClone();

            result.Trades = new List<Trade>();
            result.Trades = Trades.Select(t => t.Clone().Convert(price)).ToList();
            result.Price = result.Price / price;

            return result;
        }

        public Offer Clone()
        {
            return Clone<Offer>();
        }

        public override void Reset()
        {
            Status = OfferStatus.Pending;
        }

        public override void Finish()
        {
            if (Status == OfferStatus.Pending)
                Status = OfferStatus.New;
        }

        public void SetTrades(List<Trade> list)
        {
            this.Trades = list;
        }

        public void CleanTrades()
        {
            this.Trades = new List<Trade>();
        }
    }

    public enum OfferType
    {
        Bid,
        Ask
    }

    public enum OfferStatus
    {
        Pending,
        New,
        Canceled,
        PartillyFilled,
        Filled
    }
}