using Backoffice.API.Repositories;
using Backoffice.Shared.Models.Requests;
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

        [HttpPost("topfixtures")]
        public async Task<IActionResult> PostTopFixture(PostClientTopFixtureRequest request)
        {
            var region = "";
            var result = await _platformRepository.PostClientTopFixtures(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
    }
}
