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
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : Controller
    {
        private IChainRepository repository;

        public QuotesController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Get quotes by reference
        /// </summary>
        /// <remarks>
        /// A quote is the last price at which a asset traded, meaning the most recent price to which a buyer and seller agreed and at which some amount of the asset was transacted. More info https://www.investopedia.com/terms/q/quote.asp
        /// </remarks>
        /// <param name="reference">
        /// Shows the price and candles (volume, high, low) by reference asset
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{reference}")]
        public async Task<List<Quote>> GetQuotes([FromRoute]string reference = "BIZ")
        {
            return await repository.GetQuotes(reference);
        }
    }
}