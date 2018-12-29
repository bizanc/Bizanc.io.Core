using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Api.Controllers
{
    [Route("api/[controller]")]
    public class StatsController : Controller
    {
        private IChainRepository repository;

        public StatsController(IChainRepository repository)
        {
            this.repository = repository;
        }

        [HttpGet]
        public async Task<Stats> List()
        {
            return await repository.GetStats();
        }
    }
}