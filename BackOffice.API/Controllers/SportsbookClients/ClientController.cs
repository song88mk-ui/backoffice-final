using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.SportsbookClients
{
    [ActionFilter.Authorize]
    [Route("sportsbook/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
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

        [HttpPost("information/list")]
        public async Task<IActionResult> GetClientList([FromBody] GetClientListRequest request)
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var clientCode = JwtParser.ParseClaimsFromJwt(token).Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.ToString();

            var result = new BaseResponse<ClientListResponse>();
            if (clientCode.ToUpper() == "ALL")
                result = await _backofficeRepository.GetClientList(request);
            else
            {
                var brands = JwtParser.ParseClaimsFromJwt(token).Where(x => x.Type.ToUpper() == "CLIENTCODES").FirstOrDefault()?.Value.Split(',').ToList() ?? new List<string>();
                result = await _backofficeRepository.GetClientList(request, brands);
            }

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("post")]
        public async Task<IActionResult> AddOrUpdateClient([FromBody] ClientListResponse.Client request)
        {
            var result = await _backofficeRepository.AddOrUpdateClient(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/setting")]
        public async Task<IActionResult> GetBetSetting([FromBody] BetSettingRequest request)
        {
            var result = await _backofficeRepository.GetBetSetting(request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("exchange/rates")]
        public async Task<IActionResult> GetExchangeRates()
        {
            var result = await _backofficeRepository.GetExchangeRates();

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
