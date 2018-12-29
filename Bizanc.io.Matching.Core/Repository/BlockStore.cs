using System;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public class BlockStore
    {
        public Domain.Immutable.TransactionManager TransactManager { get; set; } = new Domain.Immutable.TransactionManager();
        public Domain.Immutable.Deposit DepositManager { get; set; } = new Domain.Immutable.Deposit();
        public Domain.Immutable.Withdrawal WithdrawalManager { get; set; } = new Domain.Immutable.Withdrawal();
        public Domain.Immutable.Book BookManager { get; private set; } = new Domain.Immutable.Book();

        public Block LastBlock { get; set; }
    }
}