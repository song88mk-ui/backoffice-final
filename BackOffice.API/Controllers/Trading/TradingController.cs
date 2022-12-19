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
        private ITradingRepository _tradingRepository { get; set; }

        #region Ctor
        public TradingController(ITradingRepository tradingRepository)
        {
            _tradingRepository = tradingRepository;
        }
        #endregion

        #region Sports
        [HttpGet("sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetSports(int fixtureTypeId)
        {
            var result = await _tradingRepository.GetSports(fixtureTypeId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }            
        }

        [HttpGet("sports/topmarkets/{sportId}")]
        public async Task<IActionResult> GetTopMarkets(int sportId)
        {
            var result = await _tradingRepository.GetTopMarkets(sportId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("sports/lookup")]
        public async Task<IActionResult> GetSportLookup()
        {
            var result = await _tradingRepository.GetSportLookup("HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Segment
        [HttpGet("segment/lookup/{sportId}")]
        public async Task<IActionResult> GetSegmentLookup(int sportId)
        {
            var result = await _tradingRepository.GetSegmentLookup(sportId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region League
        [HttpGet("league/lookup/{sportId}/{segmentId}")]
        public async Task<IActionResult> GetLeagueLookup(int sportId, int segmentId)
        {
            var result = await _tradingRepository.GetLeagueLookup(sportId, segmentId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Fixtures
        [HttpGet("fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}")]
        public async Task<IActionResult> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId)
        {
            var result = await _tradingRepository.GetFixturesByMarket(fixtureTypeId, sportId, segmentId, leagueId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}/{marketTemplateId}")]
        public async Task<IActionResult> GetMarketFixtures(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId)
        {
            var result = await _tradingRepository.GetMarketFixtures(fixtureTypeId, sportId, segmentId, leagueId, marketTemplateId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("fixtures/market/selections/{fixtureTypeId}/{providerFixtureId}/{providerMarketId}/{providerSelectionId}")]
        public async Task<IActionResult> GetFixtureMarketSelections(int fixtureTypeId, string providerFixtureId, string providerMarketId, string providerSelectionId)
        {
            var result = await _tradingRepository.GetFixtureMarketSelections(fixtureTypeId, providerFixtureId, providerMarketId, providerSelectionId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("fixtures2/market/details/{providerFixtureId}")]
        public async Task<IActionResult> GetMarketFixtureDetails(string providerFixtureId)
        {
            var result = await _tradingRepository.GetFixtureMarketDetails(providerFixtureId, "HK");

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
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
