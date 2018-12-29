using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Crypto;

namespace Bizanc.io.Matching.Core.Domain.Immutable
{
    public class Pool
    {
        public Guid Id = Guid.NewGuid();
        public EntityPool<Domain.Deposit> DepositPool { get; private set; } = new EntityPool<Domain.Deposit>();
        public EntityPool<Domain.Offer> OfferPool { get; private set; } = new EntityPool<Domain.Offer>();
        public EntityPool<Domain.OfferCancel> OfferCancelPool { get; private set; } = new EntityPool<Domain.OfferCancel>();
        public EntityPool<Domain.Transaction> TransactionPool { get; private set; } = new EntityPool<Domain.Transaction>();
        public EntityPool<Domain.Withdrawal> WithdrawalPool { get; private set; } = new EntityPool<Domain.Withdrawal>();

        public Pool() { }

        public async Task<bool> Add(Domain.Deposit dp) =>
            await DepositPool.Add(dp);

        public async Task<bool> Add(Domain.Offer of) =>
            await OfferPool.Add(of);

        public async Task<bool> Add(Domain.OfferCancel of) =>
        await OfferCancelPool.Add(of);

        public async Task<bool> Add(Domain.Transaction tx) =>
            await TransactionPool.Add(tx);

        public async Task<bool> Add(Domain.Withdrawal wd) =>
            await WithdrawalPool.Add(wd);

        public async Task Add(IEnumerable<Domain.Deposit> dp) =>
                await DepositPool.Add(dp);

        public async Task Add(IEnumerable<Domain.Offer> of) =>
            await OfferPool.Add(of);

        public async Task Add(IEnumerable<Domain.OfferCancel> of) =>
        await OfferCancelPool.Add(of);

        public async Task Add(IEnumerable<Domain.Transaction> tx) =>
            await TransactionPool.Add(tx);

        public async Task Add(IEnumerable<Domain.Withdrawal> wd) =>
                await WithdrawalPool.Add(wd);

        public async Task<Domain.Deposit> GetDeposit(string id) =>
            await DepositPool.Get(id);

        public async Task<Domain.Offer> GetOffer(string id) =>
            await OfferPool.Get(id);

        public async Task<Domain.OfferCancel> GetOfferCancel(string id) =>
        await OfferCancelPool.Get(id);

        public async Task<Domain.Transaction> GetTransaction(string id) =>
        await TransactionPool.Get(id);

        public async Task<Domain.Withdrawal> GetWithdrawal(string id) =>
            await WithdrawalPool.Get(id);

        public async Task<bool> Contains(Domain.Deposit dp) =>
            await DepositPool.Contains(dp);

        public async Task<bool> Contains(Domain.Offer of) =>
            await OfferPool.Contains(of);

        public async Task<bool> Contains(Domain.OfferCancel of) =>
        await OfferCancelPool.Contains(of);

        public async Task<bool> Contains(Domain.Transaction tx) =>
            await TransactionPool.Contains(tx);

        public async Task<bool> Contains(Domain.Withdrawal wd) =>
            await WithdrawalPool.Contains(wd);

        public async Task<bool> ContainsAll(IEnumerable<Domain.Deposit> dp) =>
        await DepositPool.ContainsAll(dp);

        public async Task<bool> ContainsAll(IEnumerable<Domain.Offer> of) =>
            await OfferPool.ContainsAll(of);

        public async Task<bool> ContainsAll(IEnumerable<Domain.OfferCancel> of) =>
        await OfferCancelPool.ContainsAll(of);

        public async Task<bool> ContainsAll(IEnumerable<Domain.Transaction> tx) =>
            await TransactionPool.ContainsAll(tx);

        public async Task<bool> ContainsAll(IEnumerable<Domain.Withdrawal> wd) =>
            await WithdrawalPool.ContainsAll(wd);

        public async Task Remove(IEnumerable<Domain.Deposit> batch) =>
             await DepositPool.Remove(batch);

        public async Task Remove(IEnumerable<Domain.Offer> batch) =>
             await OfferPool.Remove(batch);

        public async Task Remove(IEnumerable<Domain.OfferCancel> batch) =>
        await OfferCancelPool.Remove(batch);

        public async Task Remove(IEnumerable<Domain.Transaction> batch) =>
            await TransactionPool.Remove(batch);

        public async Task Remove(IEnumerable<Domain.Withdrawal> batch) =>
            await WithdrawalPool.Remove(batch);

        public Pool Fork()
        {
            var fork = new Pool();
            fork.DepositPool = DepositPool.Fork();
            fork.OfferPool = OfferPool.Fork();
            fork.OfferCancelPool = OfferCancelPool.Fork();
            fork.TransactionPool = TransactionPool.Fork();
            fork.WithdrawalPool = WithdrawalPool.Fork();

            return fork;
        }

        public async Task Merge(Pool pool)
        {
            await DepositPool.Add(await pool.DepositPool.GetPool());
            await OfferPool.Add(await pool.OfferPool.GetPool());
            await OfferCancelPool.Add(await pool.OfferCancelPool.GetPool());
            await TransactionPool.Add(await pool.TransactionPool.GetPool());
            await WithdrawalPool.Add(await pool.WithdrawalPool.GetPool());
        }
    }
}