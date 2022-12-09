using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Backoffice.Shared.Models.Response.GetTopLeaguesResponse;
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

        #region Top Leagues

        [EnableCors("AllowAllOrigins")]
        [HttpGet("topleagues/allleagues")]
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
        
        [EnableCors("AllowAllOrigins")]
        [HttpPost("topleagues")]
        public async Task<IActionResult> GetTopLeagues([FromBody] GetTopLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientsByRegion(request.ClientIdRefs);
            var result = await _platformRepository.GetTopLeagues(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("topleagues/post")]
        public async Task<IActionResult> PostTopLeagues([FromBody] PostTopLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientRegion(request.ClientIdRef);
            var result = await _platformRepository.PostTopLeagues(request, region.Result);

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
        public async Task<IActionResult> CopyTopLeagues([FromBody] CopyTopLeaguesRequest request)
        {
            var region = await _backofficeRepository.GetClientsByRegion(string.Join(",", request.ClientIds));
            var result = await _platformRepository.CopyTopLeagues(request, region.Result);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("sports")]
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
