using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models.Response;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OE.Shared.Data.Sportsbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using GroupRow = OE.Shared.Data.Sportsbook.SQLEntities.MarketTemplateGroup;

namespace Backoffice.API.Tests.Config
{
    // BO-142: the Config template editor's market groups and market list came from the old Odds Engine Trading API, which
    // does not resolve on DEV or STG (api.oddsengine.svc:3000), so the editor showed "No Markets Available". They now come
    // from the Sportsbook data: the groups from the table MarketTemplateGroup2, the market list from MongoDB MarketLookup.
    public class ConfigMarketListTests
    {
        private static SportsbookContext NewContext() =>
            new(new DbContextOptionsBuilder<SportsbookContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private static SportsbookRepository NewSportsbookRepository(SportsbookContext context)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["Custom:MongoDB-Sportsbook"] = "mongodb://localhost:27017" })
                .Build();

            return new SportsbookRepository(
                context,
                configuration,
                new Mock<IPlatformRepository>().Object,
                new Mock<IBackofficeActivityLogRepository>().Object,
                new Mock<IRabbitMqRepository>().Object,
                new Mock<IMemCache>().Object);
        }

        private static (BackofficeRepository Sut, Mock<ISportsbookRepository> Sportsbook) NewBackofficeRepository()
        {
            var name = Guid.NewGuid().ToString();
            var sportsbook = new Mock<ISportsbookRepository>();

            var sut = new BackofficeRepository(
                new BackofficeContext(new DbContextOptionsBuilder<BackofficeContext>().UseInMemoryDatabase(name).Options),
                new TranslationContext(new DbContextOptionsBuilder<TranslationContext>().UseInMemoryDatabase(name).Options),
                new Mock<IPlatformRepository>().Object,
                sportsbook.Object,
                new Mock<ITradingRepository>().Object,
                new Mock<IMemCache>().Object,
                new Mock<IRabbitMqRepository>().Object);

            return (sut, sportsbook);
        }

        private static GroupRow Row(int sportId, int fixtureStatusId, int id, string name, int sortOrder, string marketTemplates) => new()
        {
            SportId = sportId,
            FixtureStatusId = fixtureStatusId,
            Id = id,
            Name = name,
            SortOrder = sortOrder,
            MarketTemplates = marketTemplates
        };

        private static async Task<MarketTemplateGroupResponse> ReadGroups(params GroupRow[] rows)
        {
            using var context = NewContext();
            context.MarketTemplateGroup.AddRange(rows);
            await context.SaveChangesAsync();

            var response = await NewSportsbookRepository(context).GetMarketTemplateGroups();

            Assert.Equal(StatusCode.Success, response.StatusCode);

            return response.Result;
        }

        // ---- The market groups --------------------------------------------------------------------------------

        [Fact]
        public void TheGroupsAreReadFromMarketTemplateGroup2_NotTheEmptyTableOfTheSameName()
        {
            using var context = NewContext();

            Assert.Equal("MarketTemplateGroup2", context.Model.FindEntityType(typeof(GroupRow)).GetTableName());
        }

        [Fact]
        public async Task TheGroups_AreSplitIntoLiveAndPrematch_PerSport()
        {
            var result = await ReadGroups(
                Row(4, 0, 1, "Main Market", 1, "115, 147, 193"),
                Row(4, 1, 2, "Main Market", 1, "115, 7479"),
                Row(7, 0, 9, "Props", 1, "5"));

            Assert.Equal(new[] { 4, 7 }, result.Sports.Select(s => s.SportId).ToArray());

            var basketball = result.Sports[0];
            Assert.Equal(new[] { "115", "147", "193" }, Assert.Single(basketball.Prematch).MarketTemplateIds);
            Assert.Equal(new[] { "115", "7479" }, Assert.Single(basketball.Live).MarketTemplateIds);

            Assert.Single(result.Sports[1].Prematch);
            Assert.Empty(result.Sports[1].Live);
        }

        [Fact]
        public async Task AGroup_KeepsItsIdNameAndSortOrder()
        {
            var result = await ReadGroups(Row(4, 0, 5, "Quarter Markets", 4, "6598"));

            var group = Assert.Single(Assert.Single(result.Sports).Prematch);
            Assert.Equal(5, group.MarketTemplateGroupId);
            Assert.Equal("Quarter Markets", group.MarketTemplateGroupName);
            Assert.Equal(4, group.SortOrder);
        }

        [Fact]
        public async Task TheGroupsOfASport_AreOrderedBySortOrder()
        {
            var result = await ReadGroups(
                Row(4, 0, 3, "Team Props", 3, "8471"),
                Row(4, 0, 7, "Main Market", 1, "115"),
                Row(4, 0, 5, "Quarter Markets", 4, "6598"));

            Assert.Equal(new[] { "Main Market", "Team Props", "Quarter Markets" }, result.Sports[0].Prematch.Select(g => g.MarketTemplateGroupName).ToArray());
        }

        [Theory]
        [InlineData("115, 147, 193", new[] { "115", "147", "193" })]
        [InlineData("6739, 8471, ", new[] { "6739", "8471" })]
        [InlineData("7351,  14870, , 14870,", new[] { "7351", "14870" })]
        [InlineData("5, 331,8743,9162", new[] { "5", "331", "8743", "9162" })]
        [InlineData("", new string[0])]
        [InlineData(null, new string[0])]
        public async Task TheMarketTemplateIds_AreTrimmedAndDeduplicated(string stored, string[] expected)
        {
            var result = await ReadGroups(Row(4, 0, 1, "Main Market", 1, stored));

            Assert.Equal(expected, Assert.Single(Assert.Single(result.Sports).Prematch).MarketTemplateIds);
        }

        [Fact]
        public async Task WithNoRows_TheGroupListIsEmpty_NotAnError()
        {
            var result = await ReadGroups();

            Assert.Empty(result.Sports);
        }

        // ---- The BO repository wrappers -----------------------------------------------------------------------

        [Fact]
        public async Task AFailedGroupRead_IsAnErrorWithTheReason()
        {
            var (sut, sportsbook) = NewBackofficeRepository();
            sportsbook.Setup(s => s.GetMarketTemplateGroups()).ThrowsAsync(new InvalidOperationException("Sportsbook is down"));

            var response = await sut.GetConfigMarketGroups();

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("Sportsbook is down", response.Message);
        }

        [Fact]
        public async Task TheMarketLookup_IsReadForTheGivenFixtureTypeAndSport()
        {
            var (sut, sportsbook) = NewBackofficeRepository();
            sportsbook
                .Setup(s => s.GetMarketLookup(It.Is<MarketLookupRequest>(r => r.FixtureTypeId == 0 && r.SportId == 17), It.IsAny<string>()))
                .ReturnsAsync(new BaseResponse<MarketLookupResponse>
                {
                    StatusCode = StatusCode.Success,
                    Result = new MarketLookupResponse { MarketLookup = new List<MarketLookupModel> { new() { MarketTemplateId = "9484", MarketTemplateName = "4th Quarter Result" } } }
                });

            var response = await sut.GetConfigMarketLookup(0, 17);

            Assert.Equal(StatusCode.Success, response.StatusCode);
            Assert.Equal("9484", Assert.Single(response.Result.MarketLookup).MarketTemplateId);
        }

        [Fact]
        public async Task AFailedMarketLookup_IsAnErrorWithTheReason()
        {
            var (sut, sportsbook) = NewBackofficeRepository();
            sportsbook
                .Setup(s => s.GetMarketLookup(It.IsAny<MarketLookupRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new TimeoutException("MongoDB timed out"));

            var response = await sut.GetConfigMarketLookup(0, 17);

            Assert.Equal(StatusCode.Error, response.StatusCode);
            Assert.Contains("MongoDB timed out", response.Message);
        }
    }
}
