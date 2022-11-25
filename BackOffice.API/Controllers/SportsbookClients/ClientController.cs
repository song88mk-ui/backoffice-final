using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.ClientTopLeaguesResponse;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("client")]
    [EnableCors("AllowAllOrigins")]
    public class ClientController : Controller
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IPlatformRepository _platformRepository;

        public ClientController(IBackofficeRepository backofficeRepository, IPlatformRepository platformRepository)
        {
            _backofficeRepository = backofficeRepository;
            _platformRepository = platformRepository;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetClients()
        {
            var result = await _backofficeRepository.GetClients();

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #region Client Management
        [Authorize(Policy = "ManageClientsReadOnly")]
        [HttpGet("clients/{page}/{pageSize}")]
        public async Task<IActionResult> GetClientListAsync([FromRoute] int page, [FromRoute] int pageSize, [FromQuery] string businessName = "", [FromQuery] string sortOrder = "BusinessNameAscending")
        {
            var result = await _backofficeRepository.GetClients(page, pageSize, businessName, sortOrder);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Top Fixture
        [HttpGet("topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures([FromQuery] string clientIds)
        {
            var regionClients = await _backofficeRepository.GetClientsByRegion(clientIds);
            // get client top fixtures
            var result = await _platformRepository.GetClientTopFixtures(regionClients.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("topfixtures/sportstree")]
        public async Task<IActionResult> GetSportstreeByClientId([FromQuery] int clientId)
        {
            var region = await _backofficeRepository.GetClientRegion(clientId);
            // get sportstree w/ fixtures
            var result = await _platformRepository.GetSportstreeDetails(clientId, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("topfixtures")]
        public async Task<IActionResult> PostClientTopFixtures([FromBody] PostClientTopFixtureRequest request)
        {
            var region = await _backofficeRepository.GetClientRegion(request.ClientId);
            var result = await _platformRepository.PostClientTopFixtures(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("topfixtures/copy")]
        public async Task<IActionResult> CopyClientTopFixtures([FromBody] CopyClientTopFixtureRequest request)
        {
            var result = await _backofficeRepository.CopyClientTopFixtures(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion
        #region TOP LEAGUES

        [EnableCors("AllowAllOrigins")]
        [HttpGet("topleagues")]
        public async Task<IActionResult> GetClientTopLeagues(TopLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientsByRegion(request.ClientIdRefs);

            var result = await _platformRepository.GetClientTopLeagues(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("topleagues")]
        public async Task<IActionResult> AddOrUpdateTopLeagues(ClientTopLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientRegion(request.ClientIdRef);
            var result = await _platformRepository.AddOrUpdateTopLeagues(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("topleagues/copy")]
        public async Task<IActionResult> CopyClientTopLeagues([FromBody] CopyClientTopLeaguesRequest request)
        {
            var result = await _platformRepository.CopyClientTopLeagues(request);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("client/sports")]
        public async Task<IActionResult> GetClientSports([FromQuery] string LanguageCode)
        {
            var result = await _platformRepository.GetClientSports(LanguageCode, "HK");

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("client/allleagues")]
        public async Task<IActionResult> GetClientAllLeagues([FromQuery] ClientAllLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientRegion(request.ClientIdRef);

            var result = await _platformRepository.GetClientAllLeagues(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("client/currencies")]
        public async Task<IActionResult> GetCurrencies()
        {
            var result = await _backofficeRepository.GetCurrencies();

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion
    }
}
