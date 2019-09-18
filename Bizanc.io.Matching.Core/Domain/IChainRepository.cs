using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IChainRepository
    {
        Task<IList<Deposit>> ListDeposits(int size);
        Task<Deposit> GetDepositById(string id);
        Task<Deposit> GetDepositByTxHash(string txHash);
        Task<List<Deposit>> ListDepositsPool(int size);
        Task<IList<Deposit>> ListDepositsByTargetWallet(string wallet, int size);

        Task<Offer> GetOfferById(string id);
        Task<IList<Offer>> ListOffers(int size);
        Task<IList<Offer>> ListOffersPool(int size);
        Task<IList<Offer>> ListOffersByWallet(string wallet, int size);
        Task<IList<Offer>> ListOpenOffersByWallet(string wallet, string reference, int size);
        Task<bool> AppendOffer(Offer of);

        Task<bool> AppendOfferCancel(OfferCancel of);

        Task<IList<Transaction>> ListTransactions(int size);
        Task<Transaction> GetTransationById(string id);
        Task<List<Transaction>> ListTransactionsPool(int size);
        Task<IList<Transaction>> ListTransactionsBySourceWallet(string wallet, int size);
        Task<IList<Transaction>> ListTransactionsByTargetWallet(string wallet, int size);
        Task<bool> AppendTransaction(Transaction tx);

        Task<IList<Withdrawal>> ListWithdrawals(int size);
        Task<Withdrawal> GetWithdrawalById(string id);
        Task<WithdrawInfo> GetWithdrawInfoById(string id);
        Task<List<Withdrawal>> ListWithdrawalsPool(int size);
        Task<IList<Withdrawal>> ListWithdrawalsBySourceWallet(string wallet, int size);
        Task<IList<Withdrawal>> ListWithdrawalsByTargetWallet(string wallet, int size);
        Task<bool> AppendWithdrawal(Withdrawal wd);

        Task<IList<Block>> ListBlocks(int size);
        Task<Block> GetBlockByHash(string hash);
        Task<IList<Block>> ListBlocksFromDepth(long depth);

        Task<IList<Trade>> ListTradesAscending(string asset, string reference, DateTime from);

        Task<IList<Trade>> ListTradesDescending(string asset, string reference, DateTime from, int max);
        Task<List<Quote>> GetQuotes(string reference);
      
        IList<string> ListPeers();

        Task<IDictionary<string, decimal>> GetBalance(string address);

        Task<OfferBook> GetOfferBook(string asset, string reference); 

        Task<List<Candle>> GetCandle(string asset, string reference, DateTime from, CandlePeriod period = CandlePeriod.minute_1);

        List<Asset> GetAssets();

        Task<Stats> GetStats();
    }
}