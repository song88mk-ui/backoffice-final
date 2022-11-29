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
    }
}
