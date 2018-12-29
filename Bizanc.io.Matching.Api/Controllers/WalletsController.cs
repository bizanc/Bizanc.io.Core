using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Infra.Connector;
using System.Web;
using System.Net;

namespace Bizanc.io.Matching.Api.Controllers
{
    [Route("api/[controller]")]
    public class WalletsController : Controller
    {
        private IChainRepository repository;

        public WalletsController(IChainRepository repository)
        {
            this.repository = repository;
        }
        
        /// <summary>
        /// Wallet balance
        /// </summary>
        /// <remarks>
        /// Find balance of all assets in a specific Wallet
        /// </remarks>
        /// <param name="address">
        /// Address of wallet
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{address}")]
        public async Task<IDictionary<string, decimal>> GetBalance(string address)
        {
            return await repository.GetBalance(address);
        }

    }
}