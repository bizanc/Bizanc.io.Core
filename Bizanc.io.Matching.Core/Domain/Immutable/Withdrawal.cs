using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SimpleBase;
using Bizanc.io.Matching.Core.Crypto;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class Withdrawal
    {
        public TransactionManager TransactManager { get; private set; } = new TransactionManager();

        public Withdrawal()
        { }

        public Withdrawal(Withdrawal previous)
        {
            if (previous != null)
                TransactManager = previous.TransactManager;
        }

        public Withdrawal(Withdrawal previous, TransactionManager transactManager)
            : this(previous)
        {
            TransactManager = transactManager;
        }

        public bool CanProcess(Domain.Withdrawal wd)
        {
            return TransactManager.Balance.HasBalance(wd.SourceWallet, wd.Asset, wd.Size);
        }

        public Withdrawal ProcessWithdrawal(Domain.Withdrawal wd)
        {
            var transact = TransactManager;
            
            transact = transact.ChangeBalance(wd.SourceWallet, wd.Asset, -wd.Size);
            
            return new Withdrawal(this, transact);
        }

        public (Withdrawal, List<Domain.Withdrawal>, byte[] root) ProcessWithdrawals(byte[] root, IEnumerable<Domain.Withdrawal> batch)
        {
            var withdrawal = this;
            var ellegibles = new List<Domain.Withdrawal>();

            foreach (var wd in batch)
            {
                if (withdrawal.TransactManager.HasBalance(wd.SourceWallet, wd.Asset, wd.Size))
                {
                    withdrawal = withdrawal.ProcessWithdrawal(wd);
                    ellegibles.Add(wd);
                    root = CryptoHelper.Hash(Base58.Bitcoin.Encode(new Span<Byte>(root)) + wd.ToString());
                }
            }

            return (withdrawal, ellegibles, root);
        }
    }
}