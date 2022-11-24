using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("client")]
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

        #region Top Fixture
        [EnableCors("AllowAllOrigins")]
        [HttpGet("topfixtures")]
        public async Task<IActionResult> GetClientTopFixtures([FromQuery] string clientIds)
        {
            var regionClients = _backofficeRepository.GetClientsByRegion(clientIds).Result;
            // get client top fixtures
            var result = await _platformRepository.GetClientTopFixtures(regionClients);

            return Ok(result);
        }

        [EnableCors("AllowAllOrigins")]
        [HttpGet("topfixtures/sportstree")]
        public async Task<IActionResult> GetSportstreeByClientId([FromQuery] int clientId)
        {
            var region = _backofficeRepository.GetClientRegion(clientId).Result;
            // get sportstree w/ fixtures
            var result = await _platformRepository.GetSportstreeDetails(clientId, region);

            return Ok(result);
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("topfixtures")]
        public async Task<IActionResult> PostClientTopFixtures([FromBody] PostClientTopFixtureRequest request)
        {
            var region = _backofficeRepository.GetClientRegion(request.ClientId).Result;
            var result = await _platformRepository.PostClientTopFixtures(request, region);

            return Ok(result);
        }

        [EnableCors("AllowAllOrigins")]
        [HttpPost("topfixtures/copy")]
        public async Task<IActionResult> CopyClientTopFixtures([FromBody] CopyClientTopFixtureRequest request)
        {
            var result = await _backofficeRepository.CopyClientTopFixtures(request);

            return Ok(result);
        }
        #endregion
    }
}
