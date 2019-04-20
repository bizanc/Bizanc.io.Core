using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SimpleBase;
using Bizanc.io.Matching.Core.Crypto;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class Deposit
    {
        public TransactionManager TransactManager { get; private set; } = new TransactionManager();

        public Deposit()
        { }

        public Deposit(Deposit previous)
        {
            if (previous != null)
                TransactManager = previous.TransactManager;
        }

        public Deposit(Deposit previous, TransactionManager transactManager)
            : this(previous)
        {
            TransactManager = transactManager;
        }

        public Deposit ProcessDeposit(Domain.Deposit dp)
        {
            var transact = TransactManager;
            transact = transact.ChangeBalance(dp.TargetWallet, dp.Asset, dp.Quantity);
            return new Deposit(this, transact);
        }

        public (Deposit, List<Domain.Deposit>, byte[] root) ProcessDeposits(byte[] root, IEnumerable<Domain.Deposit> batch)
        {
            var deposit = this;
            var ellegibles = new List<Domain.Deposit>();

            foreach (var dp in batch)
            {
                deposit = deposit.ProcessDeposit(dp);
                ellegibles.Add(dp);
                root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + dp.ToString());
            }

            return (deposit, ellegibles, root);
        }
    }
}