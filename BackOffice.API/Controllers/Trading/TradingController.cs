using Backoffice.API.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.Trading
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradingController : ControllerBase
    {
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
    }
}
