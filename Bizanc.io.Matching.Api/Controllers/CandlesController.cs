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
    public class CandlesController : Controller
    {
        private IChainRepository repository;

        public CandlesController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Get CandleStick information
        /// </summary>
        /// <remarks>
        /// Get candlestick information is a type of price chart that displays the high, low, open and closing prices of a security for a specific period. More info in https://www.investopedia.com/terms/c/candlestick.asp
        /// </remarks>
        /// <param name="asset">
        /// Source asset represents the funds you are willing to debit from your funds
        /// </param>
        /// <param name="reference">
        /// Shows the price and candles (volume, high, low) by reference asset 
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{asset}/{reference}")]
        public async Task<List<Candle>> GetCandle([FromRoute]string asset, [FromRoute]string reference)
        {
            return await repository.GetCandle(asset, reference, DateTime.Now.AddDays(-30), CandlePeriod.minute_1);
        }

        /// <summary>
        /// Get CandleStick information by period
        /// </summary>
        /// <remarks>
        /// Get candlestick information is a type of price chart that displays the high, low, open and closing prices of a security for a specific period. More info in https://www.investopedia.com/terms/c/candlestick.asp
        /// </remarks>
        /// <param name="asset">
        /// Source asset represents the funds you are willing to debit from your funds
        /// </param>
        /// <param name="reference">
        /// Shows the price and candles (volume, high, low) by reference asset 
        /// </param>
        /// <param name="from">
        /// Initial date to query
        /// </param>
        /// <param name="period">
        /// Choose candle information by period 
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{asset}/{reference}/{from}/{period}")]
        public async Task<List<Candle>> GetCandle([FromRoute]string asset, [FromRoute]string reference, [FromRoute] DateTime from, [FromRoute]CandlePeriod period)
        {
            return await repository.GetCandle(asset, reference, from, period);
        }
    }
}