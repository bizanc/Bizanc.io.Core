using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;
using System.Globalization;

namespace Bizanc.io.Matching.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OfferBooksController : Controller
    {
        private IChainRepository repository;

        public OfferBooksController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Get offerbook by source, target and reference
        /// </summary>
        /// <remarks>
        /// Get offerbook by asset source, asset target and asset reference. 1 Source asset represents the funds you already have, 2 Target asset represents the funds you have possibility buy and 3 reference asset, shows the information based on the asset reference 
        /// </remarks>
        /// <param name="asset">
        /// Source asset represents the funds you are willing to debit from your funds
        /// </param>
        /// <param name="reference">
        /// Shows the price and candles (volume, high, low) by reference asset 
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{asset}/{reference}")]
        public async Task<OfferBook> GetOfferBook([FromRoute]string asset, [FromRoute]string reference = "BIZ")
        {
            return await repository.GetOfferBook(asset, reference);
        }
    }
}