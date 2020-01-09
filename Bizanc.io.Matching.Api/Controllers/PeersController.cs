using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeersController : Controller
    {
        private IChainRepository repository;

        public PeersController(IChainRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// Show Peer's IP address
        /// </summary>
        /// <remarks>
        /// Get Array of Peer's IP address
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet]
        public IList<string> List()
        {
            return repository.ListPeers();
        }
    }
}