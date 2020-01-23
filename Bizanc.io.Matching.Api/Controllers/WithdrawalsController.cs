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
    public class WithdrawalsController : Controller
    {
        private IChainRepository repository;

        public WithdrawalsController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// List of withdrawal
        /// </summary>
        /// <remarks>
        /// Array the information about withdrawals on Bizanc Ominichain Network
        /// </remarks>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet]
        public async Task<IList<Withdrawal>> List([FromQuery]int size = 10)
        {
            return await repository.ListWithdrawals(size);
        }

        /// <summary>
        /// Get withdrawal by ID
        /// </summary>
        /// <remarks>
        /// Find the withdrawal choose by ID - hashStr
        /// </remarks>
        /// <param name="id">
        /// Withdraw id - hashStr
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{id}")]
        public async Task<WithdrawInfoModel> Get([FromRoute]string id)
        {
            WithdrawInfoModel result = null;
            var wd = await repository.GetWithdrawalById(id);

            if (wd != null)
            {
                result = new WithdrawInfoModel()
                {
                    Asset = wd.Asset,
                    HashStr = wd.HashStr,
                    Mined = wd.Mined,
                    Signature = wd.Signature,
                    Size = wd.Size,
                    SourceWallet = wd.SourceWallet,
                    TargetWallet = wd.TargetWallet,
                    Timestamp = wd.TimeStampTicks
                };

                var info = await repository.GetWithdrawInfoById(id);
                if (info != null)
                    result.TxHash = info.TxHash;
            }

            return result;
        }

        /// <summary>
        /// List withdrawals pool 
        /// </summary>
        /// <remarks>
        /// Find withdrawals into mining pool
        /// </remarks>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Pool")]
        public async Task<IList<Withdrawal>> ListPool([FromQuery]int size = 10)
        {
            return await repository.ListWithdrawalsPool(size);
        }

        /// <summary>
        /// List withrawals by sourceWallet  
        /// </summary>
        /// <remarks>
        /// Find withdrawals into mining pool
        /// </remarks>
        /// <param name="wallet">
        /// Wallet to search for
        /// </param>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Source/{wallet}")]
        public async Task<IList<Withdrawal>> ListBySourceWallet([FromRoute]string wallet, [FromQuery]int size = 10)
        {
            return await repository.ListWithdrawalsBySourceWallet(wallet, size);
        }

        /// <summary>
        /// List withrawals by targetWallet  
        /// </summary>
        /// <remarks>
        /// Find withdrawals into mining pool
        /// </remarks>
        /// <param name="wallet">
        /// Wallet to search for
        /// </param>
        /// <param name="size">
        /// Mas size of the result
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("Target/{wallet}")]
        public async Task<IList<Withdrawal>> ListByTargetWallet([FromRoute]string wallet, [FromQuery]int size = 10)
        {
            return await repository.ListWithdrawalsByTargetWallet(wallet, size);
        }

        /// <summary>
        /// Withdrawal assets
        /// </summary>
        /// <remarks>
        /// Withdrawal assets from from Bizanc Ominichain Network and will get be credit back into the 
        /// </remarks>
        /// <param name="model">
        /// Withdrawal Data
        /// </param>
        /// <response code="200">Success</response>
        [HttpPost]
        public async Task<bool> Create([FromBody]WithdrawalModel model)
        {
            try
            {
                var wd = new Withdrawal();

                wd.SourceWallet = model.SourceWallet;
                wd.TargetWallet = model.TargetWallet;
                wd.Asset = model.Asset;
                wd.Size = model.Size;
                wd.Signature = model.Signature;
                wd.TimeStampTicks = model.Timestamp;
                wd.OracleAdrress = model.OracleAddress;
                wd.OracleFee = model.OracleFee;
                wd.BuildHash();

                return await repository.AppendWithdrawal(wd);
            }
            catch (Exception e)
            {
                e.ToString();
            }

            return false;
        }
    }
}