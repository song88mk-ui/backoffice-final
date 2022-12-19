using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.Trading
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradingController : ControllerBase
    {
        // Include "x-region" in request Header
        private ITradingRepository _tradingRepository { get; set; }

        #region Ctor
        public TradingController(ITradingRepository tradingRepository)
        {
            _tradingRepository = tradingRepository;
        }
        #endregion

        #region Methods
        [HttpGet("sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetSports(int fixtureTypeId)
        {
            //var result = await _tradingRepository.GetSports(fixtureTypeId);

            //switch (result.StatusCode)
            //{
            //    case Shared.Enums.StatusCode.Success:
            //        return Ok(result);
            //    default:
            //        return BadRequest(result);
            //}

            return Ok();
        }
        #endregion

        #region Client
        [HttpPost("activitylog/list")]
        public async Task<IActionResult> AddActivityLog([FromBody] ActivityLogModel request)
        {
            var result = await _tradingRepository.AddActivityLog(request, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/list")]
        public async Task<IActionResult> GetActivityLogs([FromBody] ActivityLogRequest request)
        {
            var result = await _tradingRepository.GetActivityLogs(request, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/list")]
        public async Task<IActionResult> GetPriceboostActivityLogs([FromBody] PriceboostActivityLogRequest request)
        {
            var result = await _tradingRepository.GetPriceboostActivityLogs(request, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Sportsbook
        #endregion

        #region Priceboost
        #endregion

        #region Activity Log
        #endregion
    }
}
