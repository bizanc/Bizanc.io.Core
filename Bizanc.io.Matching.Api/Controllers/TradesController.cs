using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;
using System.Globalization;
using Bizanc.io.Matching.Api.Model;

namespace Bizanc.io.Matching.Api.Controllers
{
    [Route("api/[controller]")]
    public class TradesController : Controller
    {
        private IChainRepository repository;

        public TradesController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Get trade by source target and reference
        /// </summary>
        /// <remarks>
        /// Get trade by source target and reference
        /// </remarks>
        /// <param name="asset">
        /// Source asset represents the funds you are willing to debit from your funds
        /// </param>
        /// <param name="reference">
        /// Shows the price and candles (volume, high, low) by reference asset 
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{asset}/{reference}")]
        public async Task<IList<Trade>> ListTrades([FromRoute]string asset, [FromRoute]string reference)
        {
            var trades = await repository.ListTrades(asset, reference, DateTime.Now.AddHours(-24));

            if (trades.Count > 10)
                return trades.Skip(trades.Count - 10).Take(10).Reverse().ToList();

            return trades.Reverse().ToList();
        }
    }
}