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
    public class OffersController : Controller
    {
        private IChainRepository repository;

        public OffersController(IChainRepository repository)
        {
            this.repository = repository;
        }
        
        /// <summary>
        /// List offers
        /// </summary>
        /// <remarks>
        /// Get offers information. Offer Status: 0 - Pending, 1 - New, 2 - Cancelled, 3 - PartillyFilled, 4 - Filled) 
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet]
        public async Task<IList<Offer>> List([FromQuery]int size = 10)
        {
            return await repository.ListOffers(size);
        }

        /// <summary>
        /// Get offer by ID
        /// </summary>
        /// <remarks>
        /// Find offers by ID. Offer Status: 0 - Pending, 1 - New, 2 - Cancelled, 3 - PartillyFilled, 4 - Filled) 
        /// </remarks>
        /// <param name="id">
        /// Offers ID - hashStr
        /// </param>
        /// <response code="200">Success</response>
        [HttpGet("{id}")]
        public async Task<Offer> Get([FromRoute]string id)
        {
            return await repository.GetOfferById(id);
        }

        /// <summary>
        /// Get offer Pool
        /// </summary>
        /// <remarks>
        /// Find offers into mining pool. Offer Status: 0 - Pending, 1 - New, 2 - Cancelled, 3 - PartillyFilled, 4 - Filled) 
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("Pool")]
        public async Task<IList<Offer>> ListPool([FromQuery]int size = 10)
        {
            return await repository.ListOffersPool(size);
        }

        /// <summary>
        /// List offers by Wallet
        /// </summary>
        /// <remarks>
        /// Find offers into by wallet. Offer Status: 0 - Pending, 1 - New, 2 - Cancelled, 3 - PartillyFilled, 4 - Filled) 
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("Wallet/{wallet}")]
        public async Task<IList<Offer>> ListByWallet([FromRoute]string wallet, [FromQuery]int size = 10)
        {
            return await repository.ListOffersByWallet(wallet, size);
        }

        /// <summary>
        /// List open offers by Wallet
        /// </summary>
        /// <remarks>
        /// Find offers into by wallet. Offer Status: 0 - Pending, 1 - New, 2 - Cancelled, 3 - PartillyFilled, 4 - Filled) 
        /// </remarks>
        /// <response code="200">Success</response>
        [HttpGet("Wallet/Open/{wallet}/{reference}")]
        public async Task<IList<Offer>> ListIOpenByWallet([FromRoute]string wallet, [FromRoute]string reference, [FromQuery]int size = 10)
        {
            return await repository.ListOpenOffersByWallet(wallet, reference, size);
        }

        /// <summary>
        /// Send new offer to book
        /// </summary>
        /// <remarks>
        /// Send new offer to specify 
        /// </remarks>
        /// <param name="model">
        /// Offer Data
        /// </param>
        /// <response code="200">Success</response>
        [HttpPost()]
        public async Task<bool> Create([FromBody]OfferModel model)
        {
            var of = new Offer();

            of.Wallet = model.Wallet;
            of.Asset = model.Asset;
            of.Type = model.Type;
            of.TimeStampTicks = model.Timestamp;
            of.Quantity = model.Quantity;
            of.Price = model.Price;
            of.Signature = model.Signature;
            of.BuildHash();

            return await repository.AppendOffer(of);
        }

        /// <summary>
        /// Send new offer to book
        /// </summary>
        /// <remarks>
        /// Send new offer to specify 
        /// </remarks>
        /// <param name="model">
        /// Offer Data
        /// </param>
        /// <response code="200">Success</response>
        [HttpDelete()]
        public async Task<bool> Delete([FromBody]OfferCancelModel model)
        {
            var of = new OfferCancel();

            of.Offer = model.Offer;
            of.Wallet = model.Wallet;
            of.TimeStampTicks = model.Timestamp;
            of.Signature = model.Signature;
            of.BuildHash();

            return await repository.AppendOfferCancel(of);
        }
    }
}