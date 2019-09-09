using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SimpleBase;
using Bizanc.io.Matching.Core.Crypto;
using Newtonsoft.Json;
using System.Threading;
using Serilog;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class TransactionManager
    {
        public Balance Balance { get; private set; } = new Balance();

        public TransactionManager()
        { }

        public TransactionManager(TransactionManager previous)
        {
            if (previous != null)
                Balance = previous.Balance;
        }

        public TransactionManager(TransactionManager previous, Balance balance)
            : this(previous)
        {
            Balance = balance;
        }

        public TransactionManager(Balance balance)
        {
            Balance = balance;
        }

        public TransactionManager ProcessTransaction(Transaction tx)
        {
            var balance = Balance;

            if (tx.Wallet != null)
                balance = balance.ChangeBalance(tx.Wallet, tx.Asset, -tx.Outputs.Sum(o => o.Size));

            foreach (var output in tx.Outputs)
            {
                try
                {
                    if (output != null && output.Wallet != null)
                        balance = balance.ChangeBalance(output.Wallet, tx.Asset, output.Size);
                    else
                    {
                        Log.Error("Transaction with invalid output:");
                        Log.Error(JsonConvert.SerializeObject(tx));
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failed to change wallet balance: \n" + e.ToString());
                    Log.Error(JsonConvert.SerializeObject(tx));
                }
            }

            return new TransactionManager(this, balance);
        }

        public (TransactionManager, List<Transaction>, byte[] root) ProcessTransactions(byte[] root, string minerWallet, IEnumerable<Transaction> transactions, CancellationTokenSource cancel)
        {
            var transact = this;
            var ellegibles = new List<Transaction>();

            var mined = new Transaction()
            {
                Timestamp = DateTime.Now,
                Asset = "BIZ",
                Outputs = new List<TransactionOutput>(){
                                new TransactionOutput{
                                    Wallet = minerWallet,
                                    Size = 750
                                }
                            }
            };

            mined.BuildHash();
            mined.Finish();

            root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + mined.ToString());

            ellegibles.Add(mined);

            foreach (var tx in transactions)
            {
                if(cancel.IsCancellationRequested)
                    return (transact, ellegibles, root);

                if (transact.CanProcess(tx))
                {
                    transact = transact.ProcessTransaction(tx);
                    var clone = tx.Clone();
                    ellegibles.Add(clone);
                    root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + tx.ToString());
                }
            }

            transact = transact.ProcessTransaction(mined);

            return (transact, ellegibles, root);
        }

        public bool CanProcess(Transaction tx)
        {
            return Balance.HasBalance(tx.Wallet, tx.Asset, tx.Outputs.Sum(o => o.Size));
        }

        public TransactionManager ChangeBalance(string wallet, string asset, decimal change)
        {
            var balance = Balance.ChangeBalance(wallet, asset, change);

            return new TransactionManager(this, balance);
        }

        public bool HasBalance(string wallet, string asset, decimal size)
        {
            return Balance.HasBalance(wallet, asset, size);
        }
    }
}