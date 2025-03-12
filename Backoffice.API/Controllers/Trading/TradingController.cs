using Backoffice.API.ActionFilter;
using Backoffice.API.Repositories;
using Backoffice.API.Telemetry;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Backoffice.API.Controllers.Trading
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(ApiKeyFilter))]
    public class TradingController : ControllerBase
    {
        private ITradingRepository _tradingRepository { get; set; }
        private readonly ActivitySource _activitySource;

        #region Ctor

        public TradingController(ITradingRepository tradingRepository, Instrumentation instrumentation)
        {
            _tradingRepository = tradingRepository;
            _activitySource = instrumentation.ActivitySource;
        }

        #endregion

        #region Sports
        [HttpGet("sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetSports(int fixtureTypeId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSports(fixtureTypeId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetTopMarkets(sportId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSportLookup(langCode, region);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSegmentLookup(sportId, langCode, region);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetLeagueLookup(sportId, segmentId, langCode, region);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
        [HttpGet("fixtures/{fixtureTypeId}/sports/{sportId}/segments/{segmentId}/leagues/{leagueId}/markets/{marketTemplateId}/most-even-line")]
        public async Task<IActionResult> GetMostEvenLineFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId, string marketTemplateId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMostEvenLineMarketFixtures(fixtureTypeId, sportId, segmentId, leagueId, marketTemplateId, region, langCode);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpGet("fixtures/market/{fixtureTypeId}/{sportId}/{segmentId}/{leagueId}")]
        public async Task<IActionResult> GetFixturesByMarket(int fixtureTypeId, int sportId, int segmentId, int leagueId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetFixturesByMarket(fixtureTypeId, sportId, segmentId, leagueId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMarketFixtures(fixtureTypeId, sportId, segmentId, leagueId, marketTemplateId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetFixtureMarketDetails(providerFixtureId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            using (var activity = _activitySource.StartActivity("api/trading/client"))
            {
                activity?.SetTag("clientCodes", HttpContext.Items["ClientCodes"]?.ToString());
                var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
                var clientCodes = HttpContext.Items["ClientCodes"]?.ToString().Split(',').ToList();
                
                var result = await _tradingRepository.GetClientList(region);

                switch (result.StatusCode)
                {
                    case Shared.Enums.StatusCode.Success:
                        if (clientCodes.FirstOrDefault() != "ALL")
                            result.Result = result.Result.Where(c => clientCodes.Contains(c.ClientCode, StringComparer.OrdinalIgnoreCase)).ToList();

                        return Ok(result);
                    default:
                        return BadRequest(result);
                }
            }
        }
        [HttpPost("client/config/all")]
        public async Task<IActionResult> GetClientConfig([FromBody] ClientConfigRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetClientConfig(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("v2/client/config")]
        public async Task<IActionResult> GetClientConfigv2([FromBody] ClientConfigRequestv2 request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetClientConfigv2(request, region);

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
        {//
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetClientSportOrderList(request, langCode, region);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetClientMarketTemplateGroups(clientCode, sportId, fixtureStatusId, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("client/markettemplate/{clientCode}/{sportId}/{fixtureStatusId}")]
        public async Task<IActionResult> GetClientMarketTemplates(string clientCode, int sportId, int fixtureStatusId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetClientMarketTemplates(clientCode, sportId, fixtureStatusId, langCode, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/markettemplate")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplatesOrder([FromBody] ClientMarketTemplateRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.AddOrUpdateClientMarketTemplatesOrder(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("client/markettemplate/copy")]
        public async Task<IActionResult> CopyClientMarketTemplatesOrder([FromBody] CopyClientMarketSortOrderRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.CopyClientMarketTemplatesOrder(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("client/markettemplategroup/post")]
        public async Task<IActionResult> AddOrUpdateClientMarketTemplateGroup([FromBody] ClientPostMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.AddOrUpdateClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/markettemplategroup/delete")]
        public async Task<IActionResult> DeleteClientMarketTemplateGroup([FromBody] ClientDeleteMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.DeleteClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        [HttpPost("client/markettemplategroup/copy")]
        public async Task<IActionResult> CopyClientMarketTemplateGroup([FromBody] ClientCopyMarketTemplateGroupRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.CopyClientMarketTemplateGroup(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }
        #endregion

        #region Bet

        [HttpPost("bet/selections")]
        public async Task<IActionResult> GetBetSelections([FromBody] BetSelectionDetailsRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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

        #region Fastodds

        [HttpGet("fastodds/sports/{fixtureTypeId}")]
        public async Task<IActionResult> GetFastOddsSports(int fixtureTypeId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            var langCode = HttpContext.Request.Headers.ContainsKey("x-language-code") ? HttpContext.Request.Headers["x-language-code"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSports(fixtureTypeId, region, langCode);

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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

        #region Manual

        [HttpPost("manual/marketsettlement")]
        public async Task<IActionResult> AddOrUpdateManualMarketSettlement([FromBody] List<ManualMarketSettlementRequest> request, [FromQuery] string userId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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

        [HttpGet("marketlookup/{fixtureTypeId}/{sportId}")]
        public async Task<IActionResult> GetMarketLookup(int fixtureTypeId, int sportId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMarketLookup(fixtureTypeId, sportId, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromBody] MessageRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

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
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMarketTemplateGroups(fixtureTypeId, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }


        [HttpPost("fixture/market/deactivate")]
        public async Task<IActionResult> DeactivateFixtureMarkets([FromBody] DeactivateFixtureMarketRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.DeactivateFixtureMarkets(request, region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        #region Provider Mapping

        [HttpGet("mapping/sports/{sportId}")]
        public async Task<IActionResult> GetSportMappingById([FromRoute] string sportId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSportMappingById(region, sportId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("mapping/segments/{segmentId}")]
        public async Task<IActionResult> GetSegmentMapping([FromRoute] string segmentId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSegmentMappingById(region, segmentId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("mapping/leagues/{leagueId}")]
        public async Task<IActionResult> GetLeagueMapping([FromRoute] string leagueId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetLeagueMappingById(region, leagueId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("mapping/fixtures/{fixtureId}")]
        public async Task<IActionResult> GetFixtureMapping([FromRoute] string fixtureId)
        {// 
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetFixtureMappingById(region, fixtureId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("mapping/markets/{marketId}")]
        public async Task<IActionResult> GetMarketMapping([FromRoute] string marketId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMarketMappingById(region, marketId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("mapping/selections/{selectionId}")]
        public async Task<IActionResult> GetSelectionMapping([FromRoute] string selectionId)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSelectionMappingById(region, selectionId);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/sports")]
        public async Task<IActionResult> GetSportMapping([FromBody] MappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;
            
            var result = await _tradingRepository.GetSportMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/segments")]
        public async Task<IActionResult> GetSegmentMapping([FromBody] MappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSegmentMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/leagues")]
        public async Task<IActionResult> GetLeagueMapping([FromBody] MappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetLeagueMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/fixtures")]
        public async Task<IActionResult> GetFixtureMapping([FromBody] MappingRequest request)
        {// 
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetFixtureMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/markets")]
        public async Task<IActionResult> GetMarketMapping([FromBody] MappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetMarketMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/selections")]
        public async Task<IActionResult> GetSelectionMapping([FromBody] MappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetSelectionMapping(region, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/sports")]
        public async Task<IActionResult> GetUnMappedSports()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedSports(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/segments")]
        public async Task<IActionResult> GetUnmappedSegments()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedSegment(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/leagues")]
        public async Task<IActionResult> GetUnmappedLeagues()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedLeague(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/fixtures")]
        public async Task<IActionResult> GetUnmappedFixtures()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedFixture(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/markets")]
        public async Task<IActionResult> GetUnmappedMarkets()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedMarket(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpGet("unmapped/selections")]
        public async Task<IActionResult> GetUnmappedSelections()
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.GetUnmappedSelection(region);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/{category}")]
        public async Task<IActionResult> AddOrUpdateProviderMapping([FromRoute] string category, [FromBody] ProviderMappingRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.AddOrUpdateProviderMapping(region, category, request);

            switch (result.StatusCode)
            {
                case Shared.Enums.StatusCode.Success:
                    return Ok(result);
                default:
                    return BadRequest(result);
            }
        }

        [HttpPost("mapping/{category}/bulk")]
        public async Task<IActionResult> AddOrUpdateProviderMappingBulk([FromRoute] string category, [FromBody] ProviderMappingBulkRequest request)
        {
            var region = HttpContext.Request.Headers.ContainsKey("x-region") ? HttpContext.Request.Headers["x-region"].ToString() : string.Empty;

            var result = await _tradingRepository.AddOrUpdateProviderMappingBulk(region, category, request);

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