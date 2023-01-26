using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Admin
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class AdminController : ControllerBase
    {
        private readonly IBackofficeRepository _backofficeRepository;
        private readonly IPlatformRepository _platformRepository;

        #region Ctor
        public AdminController(IBackofficeRepository backofficeRepository, IPlatformRepository platformRepository)
        {
            _backofficeRepository = backofficeRepository;
            _platformRepository = platformRepository;
        }        
        #endregion

        #region Bet
        [HttpPost("bet/details")]
        public async Task<IActionResult> GetManualBetDetails([FromBody] ManualBetDetailsRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _platformRepository.GetManualBetDetails(request, region);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/manualsettlement")]
        public async Task<IActionResult> ManualSettlement([FromBody] ManualBetSettlementRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _platformRepository.ManualSettlement(request, region);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/reports/manualbet/history")]
        public async Task<IActionResult> GetManualBetHistory([FromBody] ManualBetDetailHistoryRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _backofficeRepository.GetManualBetSettlementHistory(request, region);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/resendsettlement/details")]
        public async Task<IActionResult> GetResendSettlementBetDetails([FromBody] ResendSettlementBetDetailRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _platformRepository.GetResendSettlementBetDetails(request, region);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/resendsettlement")]
        public async Task<IActionResult> ResendSettlement([FromBody] ResendSettlementRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _platformRepository.ResendSettlement(request, region);

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
