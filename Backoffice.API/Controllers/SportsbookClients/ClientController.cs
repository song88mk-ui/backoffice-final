using Backoffice.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ApiController]
    [Route("sbc/client")]
    public class ClientController : Controller
    {
        private readonly IBackofficeRepository _backofficeRepository;

        public ClientController(IBackofficeRepository backofficeRepository)
        {
            _backofficeRepository = backofficeRepository;
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
