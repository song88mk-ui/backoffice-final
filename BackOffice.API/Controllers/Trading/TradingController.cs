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
            var region = "HK";
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

        [HttpGet("sports/selectormarkettemplates")]
        public async Task<IActionResult> GetSelectorMarketTemplates()
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetSelectorMarketTemplates(region);

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

        [HttpPost("tier/leagueorder/list")]
        public async Task<IActionResult> GetTierLeagueOrderList()
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetTierLeagueOrderList(region);

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
        [HttpPost("priceboost")]
        public async Task<IActionResult> AddOrUpdatePriceBoost([FromBody] List<PriceBoostConfigModel> request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdatePriceBoost(request, region);

            switch (result.StatusCode)
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
        [HttpPost("client/sportsettings")]
        public async Task<IActionResult> AddOrUpdateClientSportSettings([FromBody] ClientSportSettingRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdateClientSportSettings(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/sportorder/list")]
        public async Task<IActionResult> GetClientSportOrderList([FromBody] ClientSportOrderListRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientSportOrderList(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/sportorder/audit/list")]
        public async Task<IActionResult> GetClientSportOrderAuditList([FromBody] ClientSportOrderAuditRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();


            var result = await _tradingRepository.GetClientSportOrderAuditList(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/segmentorder/list")]
        public async Task<IActionResult> GetClientSegmentOrderList([FromBody] ClientSegmentOrderListRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();


            var result = await _tradingRepository.GetClientSegmentOrderList(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/leagueorder/list")]
        public async Task<IActionResult> GetClientLeagueOrderList([FromBody] ClientLeagueOrderListRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientLeagueOrderList(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("client/marketgroup/{clientCode}/{sportId}/{fixtureStatusId}")]
        public async Task<IActionResult> GetClientMarketTemplateGroups(string clientCode, int sportId, int fixtureStatusId)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetClientMarketTemplateGroups(clientCode, sportId, fixtureStatusId, region);

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

        #region BET
        [HttpPost("bet/selections")]
        public async Task<IActionResult> GetBetSelections([FromBody] BetSelectionDetailsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetBetSelections(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("bet/details")]
        public async Task<IActionResult> GetManualBetDetails([FromBody] ManualBetDetailsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetManualBetDetails(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region FASTODDS
        [HttpGet("fastodds/sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetFastOddsSports(int fixtureTypeId)
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

        [HttpPost("fastodds/fixtures")]
        public async Task<IActionResult> GetFixtures([FromBody] FastOddsFixturesRequest request)
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
        #endregion

        #region MANUAL
        [HttpPost("manual/marketsettlement")]
        public async Task<IActionResult> AddOrUpdateManualMarketSettlement([FromBody] List<ManualMarketSettlementRequest> request, [FromQuery] string userId)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddOrUpdateManualMarketSettlement(request, userId, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("manual/marketsettlement/details")]
        public async Task<IActionResult> GetManualMarketSettlementDetails([FromBody] ManualMarketSettlementDetailsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetManualMarketSettlementDetails(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("manual/messagesettlement")]
        public async Task<IActionResult> AddManualMessageSettlement([FromBody] List<MessageSettlementRequest> request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.AddManualMessageSettlement(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("manual/messagesettlement/list")]
        public async Task<IActionResult> GetMessageSettlementAudit([FromBody] ManualMessageSettlementRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetMessageSettlementAudit(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] MessageRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.Publish(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("bookmakers")]
        public async Task<IActionResult> GetBookmakers()
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetBookmakers(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("notifications")]
        public async Task<IActionResult> GetNotifications([FromBody] NotificationsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetNotifications(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("markettemplateplayerprops")]
        public async Task<IActionResult> GetMarketTemplatePlayerProps([FromBody] MarketTemplatePlayerPropsRequest request)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetMarketTemplatePlayerProps(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("markettemplategroups/{fixtureTypeId}")]
        public async Task<IActionResult> GetMarketTemplateGroups(int fixtureTypeId)
        {
            var region = "";
            if (HttpContext.Request.Headers.ContainsKey("x-region"))
                region = HttpContext.Request.Headers["x-region"].ToString();

            var result = await _tradingRepository.GetMarketTemplateGroups(fixtureTypeId, region);

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
