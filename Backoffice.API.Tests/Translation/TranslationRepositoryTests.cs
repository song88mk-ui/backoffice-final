using Backoffice.API.Models;
using Backoffice.API.Repositories;
using Backoffice.Shared.Data.Backoffice;
using Backoffice.Shared.Data.Translation;
using Backoffice.Shared.Data.Translation.Entities;
using Backoffice.Shared.Enums;
using Backoffice.Shared.Models.Requests;
using Backoffice.Shared.Models;
using Backoffice.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Backoffice.API.Tests.Translation
{
    public class TranslationRepositoryTests
    {
        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static BackofficeContext BuildBackofficeContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<BackofficeContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new BackofficeContext(opts);
        }

        private static TranslationContext BuildTranslationContext(string dbName)
        {
            var opts = new DbContextOptionsBuilder<TranslationContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new TranslationContext(opts);
        }

        private static BackofficeRepository BuildSut(BackofficeContext backofficeContext, TranslationContext translationContext)
        {
            return new BackofficeRepository(
                backofficeContext,
                translationContext,
                new Mock<IPlatformRepository>().Object,
                new Mock<ISportsbookRepository>().Object,
                new Mock<ITradingRepository>().Object,
                new Mock<IMemCache>().Object,
                new Mock<IRabbitMqRepository>().Object);
        }

        private static RequesterInfo MakeRequester(string page = "Translations.Static") =>
            new RequesterInfo { Page = page, UserId = "tester", UserName = "Test User" };

        // ---------------------------------------------------------------
        // GetSportsbookTranslations / AddOrUpdateSportsbookTranslation / DeleteSportsbookTranslation
        // (SportsbookStatic table, backs the Static Translation screen)
        // ---------------------------------------------------------------

        [Fact]
        public async Task GetSportsbookTranslations_ReturnsOnlyRowsForRequestedLanguage()
        {
            var dbName = nameof(GetSportsbookTranslations_ReturnsOnlyRowsForRequestedLanguage);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "1st_half", Value = "1st Half" });
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "JA", Name = "1st_half", Value = "前半" });
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookTranslations("EN");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            var row = Assert.Single(result.Result);
            Assert.Equal("EN", row.LanguageCode);
            Assert.Equal("1st Half", row.Value);
        }

        [Fact]
        public async Task AddOrUpdateSportsbookTranslation_InsertsNewRow_AndLogsCreateActivity()
        {
            var dbName = nameof(AddOrUpdateSportsbookTranslation_InsertsNewRow_AndLogsCreateActivity);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookTranslation(
                new AddOrUpdateSportsbookTranslationRequest { LanguageCode = "EN", Name = "accept", Value = "Accept" },
                MakeRequester());

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verifyTranslation = BuildTranslationContext(dbName);
            var row = Assert.Single(verifyTranslation.SportsbookStatic);
            Assert.Equal("Accept", row.Value);

            using var verifyBackoffice = BuildBackofficeContext(dbName);
            var activity = Assert.Single(verifyBackoffice.ActivityLog);
            Assert.Equal(Activity.Create.ToString(), activity.Activity);
            Assert.Equal("EN.accept", activity.Payload);
            Assert.Equal("Accept", activity.To);
        }

        [Fact]
        public async Task AddOrUpdateSportsbookTranslation_UpdatesExistingRow_InsteadOfDuplicating()
        {
            var dbName = nameof(AddOrUpdateSportsbookTranslation_UpdatesExistingRow_InsteadOfDuplicating);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "accept", Value = "Accept" });
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookTranslation(
                new AddOrUpdateSportsbookTranslationRequest { LanguageCode = "EN", Name = "accept", Value = "Accepted" },
                MakeRequester());

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            var row = Assert.Single(verify.SportsbookStatic);
            Assert.Equal("Accepted", row.Value);
        }

        [Fact]
        public async Task DeleteSportsbookTranslation_RemovesRow_WhenItExists()
        {
            var dbName = nameof(DeleteSportsbookTranslation_RemovesRow_WhenItExists);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "accept", Value = "Accept" });
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.DeleteSportsbookTranslation(
                new DeleteSportsbookTranslationRequest { LanguageCode = "EN", Name = "accept" },
                MakeRequester());

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.Empty(verify.SportsbookStatic);
        }

        [Fact]
        public async Task DeleteSportsbookTranslation_ReturnsError_WhenKeyNotFound()
        {
            var dbName = nameof(DeleteSportsbookTranslation_ReturnsError_WhenKeyNotFound);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.DeleteSportsbookTranslation(
                new DeleteSportsbookTranslationRequest { LanguageCode = "EN", Name = "does_not_exist" },
                MakeRequester());

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }

        // ---------------------------------------------------------------
        // GetSportsbookGlossary / AddOrUpdateSportsbookGlossary / DeleteSportsbookGlossary
        // (TranslationCache2 table, backs the Dynamic Translation screen)
        // ---------------------------------------------------------------

        private static TranslationCache2 MakeGlossaryRow(string stringId, string sourceText, string zh = "", string es = "", string fr = "", bool hasBetPlacement = false) =>
            new TranslationCache2
            {
                StringId = stringId,
                SourceLanguage = "EN",
                SourceText = sourceText,
                TargetTextZh = zh,
                TargetTextEs = es,
                TargetTextFr = fr,
                HasBetPlacement = hasBetPlacement,
                GlossaryVersion = 1
            };

        [Fact]
        public async Task GetSportsbookGlossary_MapsRequestedTargetLanguageColumn()
        {
            var dbName = nameof(GetSportsbookGlossary_MapsRequestedTargetLanguageColumn);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球", es: "Fútbol", fr: "Football"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                TargetLanguage = "ES",
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            var row = Assert.Single(result.Result.Glossary);
            Assert.Equal("Soccer", row.SourceText);
            Assert.Equal("Fútbol", row.TargetText);
        }

        [Fact]
        public async Task GetSportsbookGlossary_FiltersBySearchTextAcrossStringIdAndSourceText()
        {
            var dbName = nameof(GetSportsbookGlossary_FiltersBySearchTextAcrossStringIdAndSourceText);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Basketball"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                SearchText = "Soccer",
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            var row = Assert.Single(result.Result.Glossary);
            Assert.Equal("SpId:1", row.StringId);
        }

        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_InsertsNewRow_LeavingOtherLanguageColumnsEmpty()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_InsertsNewRow_LeavingOtherLanguageColumnsEmpty);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookGlossary(
                new AddOrUpdateSportsbookGlossaryRequest
                {
                    StringId = "SpId:1",
                    SourceText = "Soccer",
                    TargetLanguage = "ZH",
                    TargetText = "足球"
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            var row = Assert.Single(verify.TranslationCache2);
            Assert.Equal("足球", row.TargetTextZh);
            Assert.Equal(string.Empty, row.TargetTextEs);
            Assert.Equal(string.Empty, row.TargetTextFr);
            Assert.NotNull(row.CreatedAt);
            Assert.NotNull(row.UpdatedAt);
        }

        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_UpdatingOneLanguage_DoesNotTouchOthers()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_UpdatingOneLanguage_DoesNotTouchOthers);
            long id;
            using (var seed = BuildTranslationContext(dbName))
            {
                var entity = MakeGlossaryRow("SpId:1", "Soccer", zh: "足球", es: "", fr: "");
                seed.TranslationCache2.Add(entity);
                await seed.SaveChangesAsync();
                id = entity.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookGlossary(
                new AddOrUpdateSportsbookGlossaryRequest
                {
                    Id = id,
                    StringId = "SpId:1",
                    SourceText = "Soccer",
                    TargetLanguage = "ES",
                    TargetText = "Fútbol"
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            var row = verify.TranslationCache2.Single(t => t.Id == id);
            Assert.Equal("Fútbol", row.TargetTextEs);
            Assert.Equal("足球", row.TargetTextZh); // untouched
        }

        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_RejectsDuplicateStringId_OnCreate()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_RejectsDuplicateStringId_OnCreate);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookGlossary(
                new AddOrUpdateSportsbookGlossaryRequest { StringId = "SpId:1", SourceText = "Football" },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.Single(verify.TranslationCache2); // no duplicate inserted
        }

        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_RejectsUnsupportedTargetLanguage()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_RejectsUnsupportedTargetLanguage);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookGlossary(
                new AddOrUpdateSportsbookGlossaryRequest
                {
                    StringId = "SpId:1",
                    SourceText = "Soccer",
                    TargetLanguage = "DE", // not one of ZH/ES/FR
                    TargetText = "Fußball"
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.Empty(verify.TranslationCache2);
        }

        [Fact]
        public async Task DeleteSportsbookGlossary_RemovesRow_WhenNotBetPlacementProtected()
        {
            var dbName = nameof(DeleteSportsbookGlossary_RemovesRow_WhenNotBetPlacementProtected);
            long id;
            using (var seed = BuildTranslationContext(dbName))
            {
                var entity = MakeGlossaryRow("SpId:1", "Soccer", hasBetPlacement: false);
                seed.TranslationCache2.Add(entity);
                await seed.SaveChangesAsync();
                id = entity.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.DeleteSportsbookGlossary(new DeleteSportsbookGlossaryRequest { Id = id }, MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.Empty(verify.TranslationCache2);
        }

        [Fact]
        public async Task DeleteSportsbookGlossary_BlocksDelete_WhenHasBetPlacementIsTrue()
        {
            var dbName = nameof(DeleteSportsbookGlossary_BlocksDelete_WhenHasBetPlacementIsTrue);
            long id;
            using (var seed = BuildTranslationContext(dbName))
            {
                var entity = MakeGlossaryRow("SpId:1", "Soccer", hasBetPlacement: true);
                seed.TranslationCache2.Add(entity);
                await seed.SaveChangesAsync();
                id = entity.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.DeleteSportsbookGlossary(new DeleteSportsbookGlossaryRequest { Id = id }, MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.Single(verify.TranslationCache2); // still present -- delete was blocked
        }

        [Fact]
        public async Task DeleteSportsbookGlossary_ReturnsError_WhenIdNotFound()
        {
            var dbName = nameof(DeleteSportsbookGlossary_ReturnsError_WhenIdNotFound);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.DeleteSportsbookGlossary(new DeleteSportsbookGlossaryRequest { Id = 999 }, MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }
    }
}
