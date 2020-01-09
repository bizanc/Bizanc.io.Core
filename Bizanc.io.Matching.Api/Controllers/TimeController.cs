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
    [ApiController]
    [Route("api/[controller]")]
    public class TimeController : Controller
    {
        /// <summary>
        /// Get Server time
        /// </summary>
        /// <response code="200">Success</response>
        [HttpGet]
        public long GetTime(string address)
        {
            return DateTime.Now.ToUniversalTime().Ticks;
        }
    }
}