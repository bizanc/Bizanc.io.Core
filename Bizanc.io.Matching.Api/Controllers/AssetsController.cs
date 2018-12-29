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
    public class AssetsController : Controller
    {
        private IChainRepository repository;

        public AssetsController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Get list of assets
        /// </summary>
        /// <remarks>
        /// An asset / security is a resource with economic value that an individual, corporation or country owns or controls with the expectation that it will provide a future benefit. More info https://www.investopedia.com/terms/a/asset.asp
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet()]
        public List<Asset> Get()
        {
            return repository.GetAssets();
        }
    }
}