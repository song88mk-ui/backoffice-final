using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Backoffice.Entities;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using LeagueModel = Backoffice.Shared.Models.Response.LeagueLookupResponseV2.LeagueLookupModelV2;

namespace Backoffice.API.Tests.Config
{
    // BO-143: the OE Config page showed "Calculating..." forever, or an empty list, when a count or a league lookup
    // failed, because the repository wrapped the failure as Success (count -1) or as an empty Success. A failure is
    // now an Error with the reason, which the page turns into a visible message.
    public class ConfigCountTests
    {
        private static readonly string[] AllClients = { "ALL" };

        private static (BackofficeRepository Sut, BackofficeContext Context, Mock<ITradingRepository> Trading, Mock<IPlatformRepository> Platform) Build()
        {
            var name = Guid.NewGuid().ToString();
            var context = new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options);
            var translation = new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options);
            var trading = new Mock<ITradingRepository>();
            var platform = new Mock<IPlatformRepository>();

            var sut = new BackofficeRepository(
                context,
                translation,
                platform.Object,
                new Mock<ISportsbookRepository>().Object,
                trading.Object,
                new Mock<IMemCache>().Object,
                new Mock<IRabbitMqRepository>().Object);

            return (sut, context, trading, platform);
        }

        private static BaseResponse<LeagueLookupResponseV2> Leagues(int count) => new()
        {
            StatusCode = StatusCode.Success,
            Result = new LeagueLookupResponseV2
            {
                LeagueLookup = Enumerable.Range(1, count)
                    .Select(i => new LeagueModel { SportId = 1, SportName = "Soccer", SegmentId = 10, SegmentName = "England", LeagueId = i, LeagueName = $"League {i}" })
                    .ToList()
            }
        };

        private static BaseResponse<LeagueLookupResponseV2> LeagueLookupFailure() => new()
        {
            StatusCode = StatusCode.Error,
            Message = "Error (Name or service not known (api.oddsengine.svc:3000))"
        };

        private static BaseResponse<List<PlatformClientResponse>> Clients(params string[] codes) => new()
        {
            StatusCode = StatusCode.Success,
            Result = codes.Select(c => new PlatformClientResponse { ClientId = Guid.NewGuid(), ClientCode = c, ClientName = c }).ToList()
        };

        private static ConfigTemplateAssigned Assigned(string clientCode, int leagueId) => new()
        {
            ClientId = Guid.NewGuid(),
            ClientCode = clientCode,
            ClientName = clientCode,
            SportId = 1,
            SportName = "Soccer",
            SegmentId = 10,
            SegmentName = "England",
            LeagueId = leagueId,
            LeagueName = $"League {leagueId}",
            PrematchConfigTemplateId = 5,
            LiveConfigTemplateId = 7
        };

        // ---- My Templates count ------------------------------------------------------------------------------

        [Fact]
        public async Task MyTemplatesCount_CountsTheTemplates()
        {
            var (sut, context, _, _) = Build();
            context.ConfigTemplate.AddRange(
                new ConfigTemplate { Id = 1, Name = "Soccer [Live][Tier 1]", SportId = 1, Timestamp = DateTime.UtcNow },
                new ConfigTemplate { Id = 2, Name = "Soccer [Prematch][Tier 1]", SportId = 1, Timestamp = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var response = await sut.GetTemplateConfigCount(AllClients.ToList(), string.Empty);

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(2, response.Result);
        }

        [Fact]
        public async Task MyTemplatesCount_WhenTheDatabaseFails_IsAnError_NotSuccessWithMinusOne()
        {
            var (sut, context, _, _) = Build();
            context.Dispose(); // every query on it now throws, like a missing table did on DEV and Staging

            var response = await sut.GetTemplateConfigCount(AllClients.ToList(), string.Empty);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(response.Message));
        }

        // ---- Unassigned count --------------------------------------------------------------------------------

        [Fact]
        public async Task UnassignedCount_IsEveryLeagueOfEveryClient_MinusTheAssignedOnes()
        {
            var (sut, context, trading, platform) = Build();
            trading.Setup(t => t.GetAllLeagueLookup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Leagues(3));
            platform.Setup(p => p.GetClients(It.IsAny<string>())).ReturnsAsync(Clients("S88", "OG"));
            context.ConfigTemplateAssigned.Add(Assigned("S88", 1));
            await context.SaveChangesAsync();

            var response = await sut.GetTemplateConfigCount(AllClients.ToList(), "UnAssigned");

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(3 * 2 - 1, response.Result);
        }

        [Fact]
        public async Task UnassignedCount_WhenTheLeagueLookupFails_IsAnError_WithTheReason()
        {
            var (sut, _, trading, platform) = Build();
            trading.Setup(t => t.GetAllLeagueLookup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(LeagueLookupFailure());
            platform.Setup(p => p.GetClients(It.IsAny<string>())).ReturnsAsync(Clients("S88"));

            var response = await sut.GetTemplateConfigCount(AllClients.ToList(), "UnAssigned");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("api.oddsengine.svc", response.Message);
        }

        [Fact]
        public async Task UnassignedCount_WhenThePlatformClientListFails_IsAnError()
        {
            var (sut, _, trading, platform) = Build();
            trading.Setup(t => t.GetAllLeagueLookup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Leagues(2));
            platform
                .Setup(p => p.GetClients(It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<List<PlatformClientResponse>> { StatusCode = StatusCode.Error, Message = "Platform is down" });

            var response = await sut.GetTemplateConfigCount(AllClients.ToList(), "UnAssigned");

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Equal("Platform is down", response.Message);
        }

        // ---- Unassigned list ---------------------------------------------------------------------------------

        private static ConfigTemplateLeagueRequest ListRequest(params string[] clientCodes) => new()
        {
            ClientCodes = clientCodes.ToList(),
            SportId = -1,
            SearchText = string.Empty,
            MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
        };

        [Fact]
        public async Task UnassignedList_ListsTheLeaguesAClientHasNotBeenAssigned()
        {
            var (sut, context, trading, platform) = Build();
            trading.Setup(t => t.GetAllLeagueLookup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(Leagues(3));
            platform.Setup(p => p.GetClients(It.IsAny<string>())).ReturnsAsync(Clients("S88"));
            context.ConfigTemplateAssigned.Add(Assigned("S88", 1));
            await context.SaveChangesAsync();

            var response = await sut.GetConfigState(AllClients.ToList(), ListRequest("S88"), ConfigState.UnAssigned);

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal(new[] { 2, 3 }, response.Result.ClientConfigs.Select(c => c.LeagueId).OrderBy(i => i));
        }

        [Fact]
        public async Task UnassignedList_WhenTheLeagueLookupFails_IsAnError_NotAnEmptySuccess()
        {
            var (sut, _, trading, platform) = Build();
            trading.Setup(t => t.GetAllLeagueLookup(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(LeagueLookupFailure());
            platform.Setup(p => p.GetClients(It.IsAny<string>())).ReturnsAsync(Clients("S88"));

            var response = await sut.GetConfigState(AllClients.ToList(), ListRequest("S88"), ConfigState.UnAssigned);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("api.oddsengine.svc", response.Message);
            Assert.Null(response.Result);
        }
    }
}
