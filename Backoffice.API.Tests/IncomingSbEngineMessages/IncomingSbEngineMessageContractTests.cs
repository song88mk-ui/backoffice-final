using Backoffice.Shared.Enums;
using Backoffice.Shared.IncomingSbEngineMessages;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Backoffice.API.Tests.IncomingSbEngineMessages
{
    // Odds Engine v2 publishes OnOddsChange / OnBetStopFixture with abbreviated JSON property
    // names (FiNa, FiTyId, Ma, ...). These tests deserialize payloads shaped exactly like the
    // OEv2 wire format (OnInternalOddsChangeMessage in odds-engine-2 OE.Shared) and assert the
    // incoming models bind them - guarding against the contract drift that broke BO-97.
    public class IncomingSbEngineMessageContractTests
    {
        private const string OddsChangePayload = @"{
            ""SpId"": 1,
            ""SpNa"": ""Soccer"",
            ""SegId"": 12,
            ""SegNa"": ""Europe"",
            ""LeId"": 345,
            ""LeNa"": ""Premier League"",
            ""FiId"": 9876543210,
            ""PrFiId"": ""sr:match:555"",
            ""FiNa"": ""Arsenal vs. Chelsea"",
            ""FiStDa"": ""2026-08-20T18:30:00Z"",
            ""FiStId"": 2,
            ""FiStNa"": ""Live"",
            ""Sc"": ""1:0"",
            ""FiSp"": ""spec"",
            ""IsOfLi"": true,
            ""HoReCa"": 1,
            ""AwReCa"": 0,
            ""CuPe"": 2,
            ""CuMi"": 57,
            ""FiTyId"": 0,
            ""FiTyNa"": ""Match"",
            ""ToTi"": 90,
            ""Fa"": ""home"",
            ""SoOr"": 3,
            ""ClCo"": ""G02,TSO"",
            ""IsBo"": true,
            ""HoId"": ""sr:competitor:17"",
            ""AwId"": ""sr:competitor:38"",
            ""Ma"": [
                {
                    ""PrFiId"": ""sr:match:555"",
                    ""MaId"": 111222333,
                    ""MaTeId"": ""1"",
                    ""PrMaId"": ""sr:match:555:1:"",
                    ""Li"": ""0.5"",
                    ""MaStId"": 1,
                    ""MaStNa"": ""Active"",
                    ""MaSp"": ""total=0.5"",
                    ""MaNa"": ""1x2"",
                    ""SoOr"": 1,
                    ""ClCo"": ""G02"",
                    ""ClCoMa"": ""G02:0.05|TSO:0.07"",
                    ""Se"": [
                        {
                            ""PrFiId"": ""sr:match:555"",
                            ""MaTeId"": ""1"",
                            ""SeTeId"": ""4"",
                            ""MaSp"": ""total=0.5"",
                            ""SeId"": 777888999,
                            ""Od"": 1.85,
                            ""Pr"": 0.51,
                            ""IsAc"": true,
                            ""SeSp"": ""sel-spec"",
                            ""SeNa"": ""Arsenal"",
                            ""SoOr"": 1
                        }
                    ]
                }
            ]
        }";

        [Fact]
        public void OddsChange_FixtureFields_BindFromOev2WireNames()
        {
            var message = JsonSerializer.Deserialize<IncomingOddsChangeMessage>(OddsChangePayload);

            Assert.Equal(1, message.SportId);
            Assert.Equal("Soccer", message.SportName);
            Assert.Equal(12, message.SegmentId);
            Assert.Equal("Europe", message.SegmentName);
            Assert.Equal(345, message.LeagueId);
            Assert.Equal("Premier League", message.LeagueName);
            Assert.Equal(9876543210L, message.FixtureId);
            Assert.Equal("sr:match:555", message.ProviderFixtureId);
            Assert.Equal("Arsenal vs. Chelsea", message.FixtureName);
            Assert.Equal(new DateTime(2026, 8, 20, 18, 30, 0, DateTimeKind.Utc), message.FixtureStartDate.ToUniversalTime());
            Assert.Equal(2, message.FixtureStatusId);
            Assert.Equal("Live", message.FixtureStatusName);
            Assert.Equal("1:0", message.Score);
            Assert.Equal(2, message.CurrentPeriodId);
            Assert.Equal(57, message.CurrentMinute);
            Assert.Equal(0, message.FixtureTypeId);
            Assert.Equal(3, message.SortOrder);
        }

        [Fact]
        public void OddsChange_MarketAndSelectionFields_BindFromOev2WireNames()
        {
            var message = JsonSerializer.Deserialize<IncomingOddsChangeMessage>(OddsChangePayload);

            var market = Assert.Single(message.Markets);
            Assert.Equal("sr:match:555", market.ProviderFixtureId);
            Assert.Equal(111222333L, market.MarketId);
            Assert.Equal("1", market.MarketTemplateId);
            Assert.Equal("sr:match:555:1:", market.ProviderMarketId);
            Assert.Equal("0.5", market.Line);
            Assert.Equal(MarketStatus.Active, market.MarketStatus);
            Assert.Equal("total=0.5", market.MarketSpecifier);
            Assert.Equal("1x2", market.MarketName);
            Assert.Equal(1, market.SortOrder);
            Assert.Equal("G02", market.ClientCodes);
            Assert.Equal("G02:0.05|TSO:0.07", market.ClientCodeMargin);

            var selection = Assert.Single(market.Selections);
            Assert.Equal(777888999L, selection.SelectionId);
            Assert.Equal("4", selection.SelectionTemplateId);
            Assert.Equal(1.85m, selection.Odds);
            Assert.True(selection.IsActive);
            Assert.Equal("sel-spec", selection.SelectionSpecifier);
            Assert.Equal("Arsenal", selection.SelectionName);
            Assert.Equal(1, selection.SortOrder);
        }

        [Fact]
        public void OddsChange_FieldsNotOnOev2Wire_StayDefault()
        {
            var message = JsonSerializer.Deserialize<IncomingOddsChangeMessage>(OddsChangePayload);

            // OEv2 sends only the numeric period (CuPe); there is no period name on the wire.
            Assert.Null(message.CurrentPeriod);
            Assert.Null(message.Markets.Single().Selections.Single().ProviderSelectionId);
        }

        [Fact]
        public void BetStopFixture_FieldsBindFromOev2WireNames()
        {
            var payload = @"{
                ""id"": ""abc"",
                ""PrFiId"": ""sr:match:555"",
                ""FiId"": 9876543210,
                ""SpId"": 1,
                ""FiStId"": 2,
                ""SegId"": 12,
                ""LeId"": 345,
                ""FiTyId"": 0,
                ""FiStDa"": ""2026-08-20T18:30:00Z"",
                ""IsBetStop"": true,
                ""SetActiveMarketsToInactive"": true,
                ""CorrelationId"": ""CORR-1""
            }";

            var message = JsonSerializer.Deserialize<IncomingBetStopFixtureMessage>(payload);

            Assert.Equal(1, message.SportId);
            Assert.Equal(12, message.SegmentId);
            Assert.Equal(345, message.LeagueId);
            Assert.Equal(9876543210L, message.FixtureId);
            Assert.Equal("sr:match:555", message.ProviderFixtureId);
            Assert.Equal(0, message.FixtureTypeId);
            Assert.Equal(2, message.FixtureStatusId);
            Assert.True(message.IsBetStop);
            Assert.Equal("CORR-1", message.CorrelationId);
        }

        [Fact]
        public void BetStopGlobal_FieldsBindFromOev2WireNames()
        {
            var payload = @"{ ""IsBetStop"": true, ""CorrelationId"": ""CORR-2"" }";

            var message = JsonSerializer.Deserialize<IncomingBetStopGlobalMessage>(payload);

            Assert.True(message.IsBetStop);
            Assert.Equal("CORR-2", message.CorrelationId);
        }
    }
}
