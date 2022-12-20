using Backoffice.API.Repositories;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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

        #region Sports
        [HttpGet("sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetSports(int fixtureTypeId)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetSports(fixtureTypeId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetTopMarkets(sportId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetSportLookup(region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetSegmentLookup(sportId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetLeagueLookup(sportId, segmentId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetFixturesByMarket(fixtureTypeId, sportId, segmentId, leagueId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetMarketFixtures(fixtureTypeId, sportId, segmentId, leagueId, marketTemplateId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetFixtureMarketSelections(fixtureTypeId, providerFixtureId, providerMarketId, providerSelectionId, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetFixtureMarketDetails(providerFixtureId, region);

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
        [HttpPost("sportsbook/fixtures")]
        public async Task<IActionResult> GetFixtures([FromBody] MarketFixtureRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetFixtures(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("sportsbook/outrightfixtures")]
        public async Task<IActionResult> GetOutrightFixtures([FromBody] MarketFixtureRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetOutrightFixtures(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Priceboost
        [HttpPost("sportsbook/outrightfixtures")]
        public async Task<IActionResult> AddOrUpdatePriceBoost([FromBody] List<PriceBoostConfigModel> request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdatePriceBoost(request, region);

            switch (result.Item1.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("priceboost/list")]
        public async Task<IActionResult> GetPriceBoostConfig([FromBody] PriceBoostConfigRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetPriceBoostConfig(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("priceboost/settings/list")]
        public async Task<IActionResult> GetBoostedSettings([FromBody] BoostedSettingsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetBoostedSettings(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("priceboost/settings")]
        public async Task<IActionResult> AddOrUpdateBoostedSettings([FromBody] BoostedSettingsModel request, [FromQuery] string userId)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdateBoostedSettings(request, userId, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("priceboost/audit/list")]
        public async Task<IActionResult> GetPriceBoostAuditList([FromBody] PriceBoostConfigRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetPriceBoostAuditList(request, region);

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
        [HttpGet("client")]
        public async Task<IActionResult> GetClientList()
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientList(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/config/all")]
        public async Task<IActionResult> GetClientConfig([FromBody] ClientConfigRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientConfig(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/config")]
        public async Task<IActionResult> AddOrUpdateConfig([FromBody] ConfigSettingsModel request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdateConfig(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/config/audit")]
        public async Task<IActionResult> GetConfigLogs([FromBody] ConfigLogsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetConfigLogs(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("client/leagueconfig")]
        public async Task<IActionResult> GetFixtures([FromQuery] string clientCode)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientLeagueConfig(clientCode, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("client/marketconfig")]
        public async Task<IActionResult> GetClientMarketConfig([FromQuery] string clientCode)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientMarketConfig(clientCode, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/marketconfig/{pageSize}/{page}")]
        public async Task<IActionResult> GetClientMarketConfig(int pageSize, int page, [FromQuery] string clientCode)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientMarketConfig(pageSize, page, clientCode, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Activity Log
        [HttpPost("activitylog")]
        public async Task<IActionResult> AddClientActivityLog([FromBody] ActivityLogModel request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddActivityLog(request, region);

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
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetActivityLogs(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("activitylog/priceboostlist")]
        public async Task<IActionResult> GetPriceboostActivityLogs([FromBody] PriceboostActivityLogRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetPriceboostActivityLogs(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion
    }
}
