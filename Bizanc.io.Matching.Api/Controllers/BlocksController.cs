using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;
using System.Web;

namespace Bizanc.io.Matching.Api.Controllers
{
    [Route("api/[controller]")]
    public class BlocksController : Controller
    {
        private IChainRepository repository;

        public BlocksController(IChainRepository repository)
        {
            this.repository = repository;
        }

        // GET api/blocks
        /// <summary>
        /// List chain blocks
        /// </summary>
        /// <remarks>Array of blocks, that return header, transactions, offer, id and version. This information is used to check the transactions, blocks, offers, deposits, withdrawals, merkleRoot and others caracteristics of blocks that's is open for everybody to check, maintain and create new features.</remarks>
        /// <response code="200">Success</response>
        //// <response code="400">Product has missing/invalid values</response>
        //// <response code="500">Oops! Can't create your product right now</response>
        [HttpGet]
        public async Task<IEnumerable<Block>> List([FromQuery]int size = 10)
        {
            return await repository.ListBlocks(size);
        }

        /// <summary>
        /// Find blocks by hash
        /// </summary>
        /// <remarks>
        /// Block, that return header, transactions, offer, id and version. This information is used to check the transactions, blocks, offers, deposits, withdrawals, merkleRoot and others caracteristics of blocks that's is open for everybody to check, maintain and create new features.
        /// </remarks>
        /// <param name="hash">
        /// Get array of last blocks by size
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{hash}")]
        public async Task<Block> Get([FromRoute]string hash)
        {
            return await repository.GetBlockByHash(System.Uri.UnescapeDataString(hash));
        }

        /// <summary>
        /// List from block on chain
        /// </summary>
        /// <remarks>
        /// Brings the blocks choose from block's number
        /// </remarks>
        /// <param name="depth">
        /// Get array of last blocks by depth
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("From/{depth}")]
        public async Task<IEnumerable<Block>> ListFromDepth([FromRoute]long depth)
        {
            return await repository.ListBlocksFromDepth(depth);
        }
    }
}
