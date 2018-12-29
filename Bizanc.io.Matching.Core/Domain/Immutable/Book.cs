using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SimpleBase;
using Bizanc.io.Matching.Core.Crypto;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class Book
    {
        public string Id { get; set; }

        [JsonIgnore]
        public TransactionManager TransactManager { get; private set; } = new TransactionManager();

        public ImmutableDictionary<string, OfferBook> Dictionary { get; private set; } = new Dictionary<string, OfferBook>().ToImmutableDictionary();

        public ImmutableList<Offer> ProcessedOffers { get; private set; } = new List<Offer>().ToImmutableList();

        public ImmutableList<Trade> Trades { get; private set; } = new List<Trade>().ToImmutableList();

        public string BlockHash { get; set; }

        public Book()
        {
            Dictionary = Dictionary.Add("BTC", new OfferBook("BTC"));
            Dictionary = Dictionary.Add("ETH", new OfferBook("ETH"));
            Dictionary = Dictionary.Add("TBRL", new OfferBook("TBRL"));
        }

        public Book(Book previous)
        {
            if (previous != null)
            {
                TransactManager = previous.TransactManager;
                Dictionary = previous.Dictionary;
            }
        }

        public Book(Book previous,
                                    TransactionManager transactManager,
                                    ImmutableDictionary<string, Domain.OfferBook> dic = null,
                                    ImmutableList<Offer> processedOffers = null,
                                    ImmutableList<Trade> trades = null)
            : this(previous)
        {
            TransactManager = transactManager;
            if (dic != null)
                Dictionary = dic;

            if (processedOffers != null)
                ProcessedOffers = processedOffers;

            if (trades != null)
                Trades = trades;
        }

        public OfferBook GetBook(string asset)
        {
            if (Dictionary.ContainsKey(asset))
                return Dictionary[asset];
            else
                return new OfferBook(asset);
        }

        public (bool, Book) ProcessOffer(Domain.Offer of)
        {
            var transact = TransactManager;
            var dic = Dictionary;
            var result = false;
            var offers = new List<Offer>();
            var trades = new List<Trade>();
            var resultOffers = new List<Offer>();
            var resultTrades = new List<Trade>();

            if (!dic.ContainsKey(of.Asset))
                dic = dic.Add(of.Asset, new OfferBook(of.Asset));


            var sourceQuantity = of.Quantity;
            var asset = of.Asset;

            if (of.Type == OfferType.Bid)
            {
                sourceQuantity = of.Quantity * of.Price;
                asset = "BIZ";
            }

            if (transact.HasBalance(of.Wallet, asset, sourceQuantity))
            {
                transact = transact.ChangeBalance(of.Wallet, asset, -sourceQuantity);
                var book = dic[of.Asset];

                if (of.Type == OfferType.Bid)
                    (book, resultTrades, resultOffers) = book.AddBid(of);
                else
                    (book, resultTrades, resultOffers) = book.AddAsk(of);

                dic = dic.SetItem(of.Asset, book);
                offers.AddRange(resultOffers);
                trades.AddRange(resultTrades);

                result = true;
                foreach (var trade in resultTrades)
                {
                    transact = transact.ChangeBalance(trade.BuyerWallet, trade.Asset, trade.Quantity);
                    transact = transact.ChangeBalance(trade.SellerWallet, "BIZ", trade.Quantity * trade.Price);
                }
            }
            else
                Console.WriteLine("Wallet has no balance to send offer.");

            if (result)
                return (true, new Book(this, transact, dic, ProcessedOffers.AddRange(offers), Trades.AddRange(trades)));
            else
                return (false, this);
        }

        public (Book, List<Domain.Offer>, byte[] root) ProcessOffers(byte[] root, IEnumerable<Domain.Offer> batch)
        {
            var book = this;
            var ellegibles = new List<Domain.Offer>();

            foreach (var of in batch)
            {
                var result = false;
                var clone = of.Clone();
                (result, book) = book.ProcessOffer(clone);
                if (result)
                {
                    clone.Finish();
                    ellegibles.Add(clone);
                    root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + clone.ToString());
                }
            }

            return (book, ellegibles, root);
        }

        public (bool, Book) ProcessOfferCancel(Domain.OfferCancel of)
        {
            var transact = TransactManager;
            var dic = Dictionary;
            Offer result = null;

            foreach (var book in dic.Values)
            {
                var offer = book.GetOffer(of.Offer);

                if (offer != null)
                {
                    if (offer.Wallet != of.Wallet)
                    {
                        Console.WriteLine("Invalide offer cancel, not owner");
                        return (false, this);
                    }

                    OfferBook newBook = null;
                    (newBook, result) = book.CancelOffer(of.Offer);
                    result.Status = OfferStatus.Canceled;

                    dic = dic.SetItem(book.Asset, newBook);

                    if (offer.Type == OfferType.Bid)
                        transact = transact.ChangeBalance(offer.Wallet, "BIZ", offer.LeavesQuantity * offer.Price);
                    else
                        transact = transact.ChangeBalance(offer.Wallet, offer.Asset, offer.LeavesQuantity);

                    return (true, new Book(this, transact, dic, ProcessedOffers.Add(result), Trades));
                }
            }

            return (false, this);
        }

        public (Book, List<Domain.OfferCancel>, byte[] root) ProcessOfferCancels(byte[] root, IEnumerable<Domain.OfferCancel> batch)
        {
            var book = this;
            var ellegibles = new List<Domain.OfferCancel>();

            foreach (var of in batch)
            {
                var result = false;
                var clone = of.Clone();
                (result, book) = book.ProcessOfferCancel(clone);
                if (result)
                {
                    clone.Finish();
                    ellegibles.Add(clone);
                    root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + clone.ToString());
                }
            }

            return (book, ellegibles, root);
        }

        public List<Offer> GetOpenOffers(string wallet)
        {
            var result = new List<Offer>();

            foreach (var book in Dictionary.Values)
                result.AddRange(book.GetOffers(wallet));

            return result;
        }

        public Offer GetOpenOffer(string hash)
        {
            Offer result = null;

            foreach (var book in Dictionary.Values)
            {
                result = book.GetOffer(hash);
                if (result != null)
                    return result;
            }

            return result;
        }

        public bool ContainsOffer(Offer of)
        {
            return GetOpenOffer(of.HashStr) != null;
        }

        public List<Offer> GetProcessedOffers(string wallet)
        {
            return ProcessedOffers.FindAll(o => o.Wallet == wallet).ToList();
        }

        public Offer GetProcessedOffer(string hash)
        {
            return ProcessedOffers.FindAll(o => o.HashStr == hash).FirstOrDefault();
        }

        public List<Trade> GetTrades(string asset)
        {
            return Trades.FindAll(t => t.Asset == asset).ToList();
        }
    }
}