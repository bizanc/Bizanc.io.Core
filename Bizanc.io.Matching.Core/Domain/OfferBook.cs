using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace Bizanc.io.Matching.Core.Domain
{
    public class OfferBook
    {
        public string Asset { get; set; }

        public decimal LastPrice { get; set; } = 1;

        public ImmutableList<Offer> Bids { get; private set; } = new List<Offer>().ToImmutableList();

        public ImmutableList<Offer> Asks { get; private set; } = new List<Offer>().ToImmutableList();

        public decimal BestBid { get { return Bids.Count > 0 ? Bids[0].Price : 1; } }

        public decimal BestAsk { get { return Asks.Count > 0 ? Asks[0].Price : 1; } }

        public OfferBook() { }
        public OfferBook(string asset)
        {
            Asset = asset;
        }

        public OfferBook(string asset, ImmutableList<Offer> bids, ImmutableList<Offer> asks, decimal lastPrice)
        : this(asset)
        {
            this.Bids = bids;
            this.Asks = asks;
            this.LastPrice = lastPrice;
        }

        public List<Offer> GetOffers(string wallet)
        {
            var result = new List<Offer>();

            result.AddRange(Bids.FindAll(o => o.Wallet == wallet));
            result.AddRange(Asks.FindAll(o => o.Wallet == wallet));

            return result;
        }

        public Offer GetOffer(string hash)
        {
            Offer result = null;

            result = Bids.FirstOrDefault(o => o.HashStr == hash);

            if (result == null)
                result = Asks.FirstOrDefault(o => o.HashStr == hash);

            return result;
        }

        public (OfferBook, Offer) CancelOffer(string hash)
        {
            for (int i = 0; i < Bids.Count; i++)
                if (Bids[i].HashStr == hash)
                    return (new OfferBook(Asset, Bids.RemoveAt(i), Asks, LastPrice), Bids[i]);

            for (int i = 0; i < Asks.Count; i++)
                if (Asks[i].HashStr == hash)
                    return (new OfferBook(Asset, Bids, Asks.RemoveAt(i), LastPrice), Asks[i]);

            return (this, null);
        }

        public (OfferBook, List<Trade>, List<Offer>) AddBid(Offer offer)
        {
            var newAsks = Asks;
            var newBids = Bids;
            var newTrades = new List<Trade>();
            var finishOffers = new List<Offer>();
            var lastPrice = LastPrice;

            foreach (var off in Asks)
            {
                var of = off.Clone();

                if (offer.Price >= of.Price && offer.LeavesQuantity > 0)
                {
                    if (of.LeavesQuantity >= offer.LeavesQuantity)
                    {
                        var execQty = offer.LeavesQuantity;

                        var trade = new Trade()
                        {
                            Buyer = offer.HashStr,
                            BuyerWallet = offer.Wallet,
                            Seller = of.HashStr,
                            SellerWallet = of.Wallet,
                            Quantity = execQty,
                            DtTrade = DateTime.Now,
                            Price = of.Price,
                            Asset = Asset
                        };
                        lastPrice = trade.Price;

                        offer.AddTrade(trade.Clone());
                        of.AddTrade(trade.Clone());
                        newTrades.Add(trade);

                        if (of.LeavesQuantity == 0)
                        {
                            newAsks = newAsks.RemoveAt(0);
                            of.Status = OfferStatus.Filled;
                            finishOffers.Add(of);
                        }
                        else
                        {
                            of.Status = OfferStatus.PartillyFilled;
                            newAsks = newAsks.SetItem(0, of);
                        }

                        offer.Status = OfferStatus.Filled;
                        finishOffers.Add(offer);
                    }
                    else
                    {
                        var execQty = of.LeavesQuantity;

                        var trade = new Trade()
                        {
                            Buyer = offer.HashStr,
                            BuyerWallet = offer.Wallet,
                            Seller = of.HashStr,
                            SellerWallet = of.Wallet,
                            Quantity = execQty,
                            DtTrade = DateTime.Now,
                            Price = of.Price,
                            Asset = Asset
                        };
                        lastPrice = trade.Price;

                        offer.AddTrade(trade.Clone());
                        of.AddTrade(trade.Clone());
                        newTrades.Add(trade);
                        finishOffers.Add(of);
                        offer.Status = OfferStatus.PartillyFilled;
                        of.Status = OfferStatus.Filled;
                        newAsks = newAsks.RemoveAt(0);
                    }
                }
                else
                    break;
            }

            if (offer.LeavesQuantity > 0)
            {
                int i = 0;
                for (i = 0; i < newBids.Count && newBids[i].Price >= offer.Price; i++)
                {

                }

                newBids = newBids.Insert(i, offer);
            }

            return (new OfferBook(Asset, newBids, newAsks, lastPrice), newTrades, finishOffers);
        }

        public OfferBook Convert(decimal refPrice)
        {
            var newAsks = new List<Offer>();
            var newBids = new List<Offer>();
            var newTrades = new List<Trade>();

            foreach (var off in Bids)
            {
                var of = off.Clone();
                of.Price = of.Price / refPrice;
                newBids.Add(of);
            }

            foreach (var off in Asks)
            {
                var of = off.Clone();
                of.Price = of.Price / refPrice;
                newAsks.Add(of);
            }

            return new OfferBook(Asset, newBids.ToImmutableList(), newAsks.ToImmutableList(), LastPrice/refPrice);
        }

        public (OfferBook, List<Trade>, List<Offer>) AddAsk(Offer offer)
        {
            var newAsks = Asks;
            var newBids = Bids;
            var newTrades = new List<Trade>();
            var finishOffers = new List<Offer>();
            var lastPrice = LastPrice;

            foreach (var off in Bids)
            {
                var of = off.Clone();

                if (offer.Price <= of.Price && offer.LeavesQuantity > 0)
                {
                    if (of.LeavesQuantity >= offer.LeavesQuantity)
                    {
                        var execQty = offer.LeavesQuantity;

                        var trade = new Trade()
                        {
                            Buyer = of.HashStr,
                            BuyerWallet = of.Wallet,
                            Seller = offer.HashStr,
                            SellerWallet = offer.Wallet,
                            Quantity = execQty,
                            DtTrade = DateTime.Now,
                            Price = of.Price,
                            Asset = Asset
                        };
                        lastPrice = trade.Price;

                        offer.AddTrade(trade.Clone());
                        of.AddTrade(trade.Clone());
                        newTrades.Add(trade);

                        if (of.LeavesQuantity == 0)
                        {
                            newBids = newBids.RemoveAt(0);
                            of.Status = OfferStatus.Filled;
                            finishOffers.Add(of);
                        }
                        else
                        {
                            of.Status = OfferStatus.PartillyFilled;
                            newBids = newBids.SetItem(0, of);
                        }

                        offer.Status = OfferStatus.Filled;
                        finishOffers.Add(offer);
                    }
                    else
                    {
                        var execQty = of.LeavesQuantity;

                        var trade = new Trade()
                        {
                            Buyer = of.HashStr,
                            BuyerWallet = of.Wallet,
                            Seller = offer.HashStr,
                            SellerWallet = offer.Wallet,
                            Quantity = execQty,
                            DtTrade = DateTime.Now,
                            Price = of.Price,
                            Asset = Asset
                        };
                        lastPrice = trade.Price;

                        offer.AddTrade(trade.Clone());
                        of.AddTrade(trade.Clone());
                        newTrades.Add(trade);
                        offer.Status = OfferStatus.PartillyFilled;
                        of.Status = OfferStatus.Filled;
                        newBids = newBids.RemoveAt(0);
                        finishOffers.Add(of);
                    }
                }
                else
                    break;
            }

            if (offer.LeavesQuantity > 0)
            {
                int i = 0;
                for (i = 0; i < newAsks.Count && newAsks[i].Price <= offer.Price; i++)
                {

                }

                newAsks = newAsks.Insert(i, offer);
            }

            return (new OfferBook(Asset, newBids, newAsks, lastPrice), newTrades, finishOffers);
        }
    }
}