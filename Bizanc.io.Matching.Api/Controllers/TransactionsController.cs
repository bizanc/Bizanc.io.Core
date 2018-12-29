using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using System.Web;
using Bizanc.io.Matching.Api.Model;

namespace Bizanc.io.Matching.Api.Controllers
{
    [Route("api/[controller]")]
    public class TransactionsController : Controller
    {
        private IChainRepository repository;

        public TransactionsController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// List transactions
        /// </summary>
        /// <remarks>
        /// Array of all transactions mined or open (not mined).
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet]
        public async Task<IList<Transaction>> List([FromQuery]int size = 10)
        {
            return await repository.ListTransactions(size);
        }

        /// <summary>
        /// Find transaction by ID
        /// </summary>
        /// <remarks>
        /// Check if the transaction has been mined or not choosen by ID - HashStr
        /// </remarks>
        /// <param name="id">
        /// Transaction id - hashStr
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{id}")]
        public async Task<Transaction> Get([FromRoute]string id)
        {
            return await repository.GetTransationById(id);
        }

        /// <summary>
        /// List transactions to be minned
        /// </summary>
        /// <remarks>
        /// Array of transactions on mining pool to be mined.
        /// </remarks>
        /// <param name="size">
        /// Number of transactions to be returned.
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Pool")]
        public async Task<IEnumerable<Transaction>> ListPool([FromQuery] int size = 10)
        {
            return await repository.ListTransactionsPool(size);
        }

        /// <summary>
        /// List transactions by source wallet
        /// </summary>
        /// <remarks>
        /// Array of transactions by source wallet
        /// </remarks>
        /// <param name="wallet">
        /// Is the target wallet that you want to check transactions
        /// </param>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Source/{wallet}")]
        public async Task<IList<Transaction>> ListBySourceWallet([FromRoute]string wallet, [FromQuery] int size = 10)
        {
            return await repository.ListTransactionsBySourceWallet(wallet, size);
        }

        /// <summary>
        /// List transactions by target wallet
        /// </summary>
        /// <remarks>
        /// Array of transactions by target wallet
        /// </remarks>
        /// <param name="wallet">
        /// Is the target wallet that you want to check transactions
        /// </param>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Target/{wallet}")]
        public async Task<IList<Transaction>> ListByTargetWallet([FromRoute]string wallet, [FromQuery]int size = 10)
        {
            return await repository.ListTransactionsByTargetWallet(wallet, size);
        }

        /// <summary>
        /// Create new transaction to mined
        /// </summary>
        /// <remarks>
        /// Create transactions to be mined on mining pool
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpPost]
        public async Task<bool> Create([FromBody] TransactionModel transaction)
        {
            try
            {
                var tx = new Transaction();

                tx.TimeStampTicks = transaction.Timestamp;
                tx.Version = "1";
                tx.Wallet = transaction.SourceWallet;
                tx.Asset = transaction.Asset;
                tx.Outputs.Add(new TransactionOutput()
                {
                    Wallet = transaction.TargetWallet,
                    Size = transaction.Size
                });
                tx.Signature = transaction.Signature;

                tx.BuildHash();

                return await repository.AppendTransaction(tx);
            }
            catch (Exception e)
            {
                e.ToString();
            }

            return false;
        }
    }
}