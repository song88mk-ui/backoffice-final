using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using BoStatusCode = Backoffice.Shared.Enums.StatusCode;

namespace Backoffice.API.Controllers.Backoffice
{
    // The class-level [Authorize] only checks the login. Each action that changes a bet also requires the
    // claim of the page that calls it (BO-130). The class-level one carries no claim, so the two do not
    // conflict. "details" stays login-only: it is a read shared by the Manual Settlement, Market Settlement
    // and Cancel Bets pages, and stacked [Authorize] attributes all have to pass.
    [Authorize]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class BetController : ControllerBase
    {
        private readonly IPlatformRepository _platformRepository;
        private readonly bool _isStaging;

        public BetController(IPlatformRepository platformRepository, IConfiguration configuration)
        {
            _platformRepository = platformRepository;
            _isStaging = bool.Parse(configuration["IsStaging"]);
        }

        #region Bet
        [HttpPost("details")]
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

        [HttpPost("manualsettlement")]
        [Authorize("Admin.Settlements.ManualBetSettlement", (int)Access.Edit)]
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

        [HttpPost("resendsettlement/details")]
        [Authorize("Admin.Settlements.ResendSettlement", (int)Access.View)]
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

        [HttpPost("resendsettlement")]
        [Authorize("Admin.Settlements.ResendSettlement", (int)Access.Edit)]
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

        [HttpPost("resendprovidercancellation")]
        [Authorize("Admin.Settlements.ResendSettlement", (int)Access.Edit)]
        public async Task<IActionResult> ResendProviderCancellation([FromBody] ResendProviderCancellationRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _platformRepository.ResendProviderCancellation(request, region);

            switch (result.StatusCode)
            {
                case BoStatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("cancel")]
        [Authorize("Admin.CancelBets", (int)Access.Edit)]
        public async Task<IActionResult> CancelBet([FromBody] BetModifyRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = new BaseResponse<BetModifyResponse>();
            if (string.IsNullOrEmpty(region))
            {
                result = await _platformRepository.CancelBet(request, "HK");

                if (!_isStaging)
                {
                    result = await _platformRepository.CancelBet(request, "SA");
                }
            }
            else
            {
                result = await _platformRepository.CancelBet(request, region);
            }

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
