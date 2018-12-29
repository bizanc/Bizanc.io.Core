using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class Balance
    {
        public ImmutableDictionary<string, WalletBalance> WalletDictionary { get; private set; } = new Dictionary<string, WalletBalance>().ToImmutableDictionary();

        public string BlockHash { get; set; }

        public string Id { get; set; }

        public Balance()
        { }

        private Balance(Balance previous)
        {
            if (previous != null)
                WalletDictionary = previous.WalletDictionary;
        }

        private Balance(Balance previous, ImmutableDictionary<string, WalletBalance> dictionary)
            : this(previous)
        {
            if (dictionary != null)
                WalletDictionary = dictionary;
        }

        public Balance ChangeBalance(string wallet, string asset, decimal change)
        {
            var dictionary = WalletDictionary;

            if (dictionary.ContainsKey(wallet))
                dictionary = dictionary.SetItem(wallet, dictionary[wallet].ChangeBalance(asset, change));
            else
            {
                var balance = new WalletBalance(wallet);
                balance = balance.ChangeBalance(asset, change);
                dictionary = dictionary.Add(wallet, balance);
            }

            return new Balance(this, dictionary);
        }

        public bool HasBalance(string wallet, string asset, decimal size)
        {
            if (size == 0)
                return true;

            if (!WalletDictionary.ContainsKey(wallet))
                return false;

            return WalletDictionary[wallet].HasBalance(asset, size);
        }
    }
}