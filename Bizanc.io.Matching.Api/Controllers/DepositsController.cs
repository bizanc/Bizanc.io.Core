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
    [ApiController]
    [Route("api/[controller]")]
    public class DepositsController : Controller
    {
        private IChainRepository repository;

        public DepositsController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// List of withdrawal
        /// </summary>
        /// <remarks>
        /// Array the information about withdrawals on Bizanc Ominichain Network
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet]
        public async Task<IList<Deposit>> List([FromQuery]int size = 10)
        {
            return await repository.ListDeposits(size);
        }

        /// <summary>
        /// Get Deposit by id
        /// </summary>
        /// <remarks>
        /// Deposit information by specific id - hashStr
        /// </remarks>
        /// <param name="id">
        /// Deposits id - hashStr
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{id}")]
        public async Task<Deposit> Get([FromRoute]string id)
        {
            return await repository.GetDepositById(id);
        }

        /// <summary>
        /// Get Deposit by txHash
        /// </summary>
        /// <remarks>
        /// Deposit information by specific txHash
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("ByTxHash/{txHash}")]
        public async Task<Deposit> GetByTxHash([FromRoute]string txHash)
        {
            return await repository.GetDepositByTxHash(txHash);
        }

        /// <summary>
        /// Get pool information
        /// </summary>
        /// <remarks>
        /// Deposit information by specific txHash
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("Pool")]
        public async Task<IList<Deposit>> ListPool([FromQuery]int size = 10)
        {
            return await repository.ListDepositsPool(size);
        }

        /// <summary>
        /// List deposits into targetWallet
        /// </summary>
        /// <remarks>
        /// Find deposits transactions by targetWallet
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("Target/{wallet}")]
        public async Task<IList<Deposit>> ListByTargetWallet([FromRoute]string wallet, [FromQuery]int size = 10)
        {
            return await repository.ListDepositsByTargetWallet(wallet, size);
        }
    }
}