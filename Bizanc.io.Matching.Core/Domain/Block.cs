using System;
using System.Collections.Generic;
using Bizanc.io.Matching.Core.Crypto;
using Bizanc.io.Matching.Core.Domain.Messages;
using Newtonsoft.Json;
using SimpleBase;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Block : BaseMessage
    {
        public override MessageType MessageType { get { return MessageType.Block; } }

        public override byte[] Hash
        {
            get
            {
                return Header.Hash;
            }
            set
            {
                Header.Hash = value;
            }
        }

        public override string HashStr { get { return Header.Hash != null ? Base58.Bitcoin.Encode(new Span<Byte>(Header.Hash)) : ""; } }

        public override DateTime Timestamp { get { return Header.TimeStamp; } set { } }
        public override long TimeStampTicks { get { return Header.TimeStampTicks; } set { } }

        public string PreviousHashStr { get { return Header.PreviousBlockHash != null ? Base58.Bitcoin.Encode(new Span<Byte>(Header.PreviousBlockHash)) : ""; } }

        public BlockHeader Header { get; set; } = new BlockHeader();

        [JsonIgnore]
        public Dictionary<string, Transaction> TransactionsDictionary { get; set; } = new Dictionary<string, Transaction>();
        [JsonIgnore]
        public Dictionary<string, Offer> OffersDictionary { get; set; } = new Dictionary<string, Offer>();
        [JsonIgnore]
        public Dictionary<string, OfferCancel> OffersCancelDictionary { get; set; } = new Dictionary<string, OfferCancel>();
        [JsonIgnore]
        public Dictionary<string, Deposit> DepositsDictionary { get; set; } = new Dictionary<string, Deposit>();
        [JsonIgnore]
        public Dictionary<string, Withdrawal> WithdrawalsDictionary { get; set; } = new Dictionary<string, Withdrawal>();

        private IEnumerable<Transaction> transactions = new List<Transaction>();
        [JsonProperty]
        public IEnumerable<Transaction> Transactions
        {
            get { return transactions; }
            set
            {
                transactions = value;
                TransactionsDictionary = new Dictionary<string, Transaction>();
                foreach (var v in value)
                    TransactionsDictionary.Add(v.HashStr, v);
            }
        }

        IEnumerable<Deposit> deposits = new List<Deposit>();
        [JsonProperty]
        public IEnumerable<Deposit> Deposits
        {
            get { return deposits; }
            set
            {
                deposits = value;
                DepositsDictionary = new Dictionary<string, Deposit>();
                foreach (var v in value)
                    DepositsDictionary.Add(v.HashStr, v);
            }
        }

        IEnumerable<Offer> offers = new List<Offer>();
        [JsonProperty]
        public IEnumerable<Offer> Offers
        {
            get { return offers; }
            set
            {
                offers = value;
                OffersDictionary = new Dictionary<string, Offer>();
                foreach (var v in value)
                    OffersDictionary.Add(v.HashStr, v);
            }
        }

        IEnumerable<OfferCancel> offerCancels = new List<OfferCancel>();
        [JsonProperty]
        public IEnumerable<OfferCancel> OfferCancels
        {
            get { return offerCancels; }
            set
            {
                offerCancels = value;
                OffersCancelDictionary = new Dictionary<string, OfferCancel>();
                foreach (var v in value)
                    OffersCancelDictionary.Add(v.HashStr, v);
            }
        }

        IEnumerable<Withdrawal> withdrawals = new List<Withdrawal>();
        [JsonProperty]
        public IEnumerable<Withdrawal> Withdrawals
        {
            get { return withdrawals; }
            set
            {
                withdrawals = value;
                WithdrawalsDictionary = new Dictionary<string, Withdrawal>();
                foreach (var v in value)
                    WithdrawalsDictionary.Add(v.HashStr, v);
            }
        }

        public void BuildMerkleRoot()
        {
            var root = new byte[0];

            foreach (var tx in Transactions)
            {
                root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + tx.ToString());
            }
            foreach (var of in Offers)
            {
                root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + of.ToString());
            }

            Header.MerkleRoot = root;
        }

        public void BuildDictionary()
        {
            Transactions = transactions;
            Offers = offers;
            OfferCancels = offerCancels;
            Deposits = deposits;
            Withdrawals = withdrawals;
        }

        public override string ToString()
        {
            var result = Header.ToString();

            foreach (var tx in Transactions)
            {
                result += tx.ToString();
            }
            foreach (var of in Offers)
            {
                result += of.ToString();
            }

            return result;
        }
    }
}