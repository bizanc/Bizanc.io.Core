using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class WalletBalance
    {
        public string Wallet { get; private set; }

        public ImmutableDictionary<string, decimal> Balance { get; private set; } = new Dictionary<string, decimal>().ToImmutableDictionary();

        public WalletBalance(string wallet)
        {
            this.Wallet = wallet;
        }

        private WalletBalance(string wallet, WalletBalance previous)
            : this(wallet)
        {
            if (previous != null)
                Balance = previous.Balance;
        }

        private WalletBalance(string wallet, WalletBalance previous, ImmutableDictionary<string, decimal> dictionary)
            : this(wallet, previous)
        {
            Balance = dictionary;
        }

        public WalletBalance ChangeBalance(string asset, decimal change)
        {
            var dictionary = Balance;

            if (dictionary.ContainsKey(asset))
                dictionary = dictionary.SetItem(asset, dictionary[asset] + change);
            else
                dictionary = dictionary.Add(asset, change);

            return new WalletBalance(Wallet, this, dictionary);
        }

        public bool HasBalance(string asset, decimal size)
        {
            if (!Balance.ContainsKey(asset))
                return false;

            return Balance[asset] >= size;
        }
    }
}