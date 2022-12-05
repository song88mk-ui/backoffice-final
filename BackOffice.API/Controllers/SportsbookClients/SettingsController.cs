using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.ClientTopLeaguesResponse;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [Authorize]
    [Route("sportsbook/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class SettingsController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IPlatformRepository _platformRepository;
        public SettingsController(IBackofficeRepository backofficeRepository, IPlatformRepository platformRepository)
        {
            _backofficeRepository = backofficeRepository;
            _platformRepository = platformRepository;
        }

        #region Top Fixture
        [HttpPost("topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures([FromBody] GetClientTopFixtureRequest request)
        {
            var regionClients = await _backofficeRepository.GetClientsByRegion(request.ClientIds);
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

        [HttpPost("topfixtures/post")]
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
            var result = await _backofficeRepository.CopyClientTopLeagues(request);

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

        #region PARLAY BONUS

        [EnableCors("AllowAllOrigins")]
        [HttpGet("parlaybonus/list")]
        public async Task<IActionResult> GetClientParlayBonus([FromQuery] GetParlayBonusRequest request)
        {
            var regionClients = await _backofficeRepository.GetClientsByRegion(request.ClientIdRef.ToString());

            var result = await _platformRepository.GetClientParlayBonus(request, regionClients.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("parlaybonus")]
        public async Task<IActionResult> AddOrUpdateParlayBonus([FromBody] ParlayBonusRequest request)
        {
            var region = await _backofficeRepository.GetClientRegion(request.ClientIdRef);

            var result = await _platformRepository.AddOrUpdateParlayBonus(request, region.Result);

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
