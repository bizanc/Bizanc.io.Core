using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Trade
    {
        public string Asset { get; set; }

        public virtual DateTime DtTrade { get; set; }

        public virtual string Buyer { get; set; }

        public virtual string BuyerWallet { get; set; }

        public virtual string Seller { get; set; }

        public virtual string SellerWallet { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual decimal Price { get; set; }

        public Trade Clone()
        {
            return (Trade)MemberwiseClone();
        }

        public Trade Reverse()
        {
            var clone = Clone();
            clone.Quantity = Quantity * Price;
            clone.Price = 1 / Price;
            return clone;
        }

        public Trade Convert(decimal price)
        {
            var clone = Clone();
            clone.Price = Price / price;
            return clone;
        }

        public bool Equals(Trade trade)
        {
            return this.Asset == trade.Asset &&
                    this.Buyer == trade.Buyer &&
                    this.BuyerWallet == trade.BuyerWallet &&
                    this.Price == trade.Price &&
                    this.Quantity == trade.Quantity &&
                    this.Seller == trade.Seller &&
                    this.SellerWallet == trade.SellerWallet;
        }

        public override string ToString()
        {
            return Asset + DtTrade.ToUniversalTime().Ticks + Buyer + BuyerWallet +
                Seller + SellerWallet + Quantity.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US")) +
                Price.ToString("0.0000000000000000000000000", CultureInfo.GetCultureInfo("En-US"));
        }
    }
}