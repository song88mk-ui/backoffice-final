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
using System.Collections.Generic;
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

        private static TranslationCache2 MakeGlossaryRow(string stringId, string sourceText, string zh = "", string es = "", string fr = "", bool hasBetPlacement = false, bool processed = false) =>
            new TranslationCache2
            {
                StringId = stringId,
                SourceLanguage = "EN",
                SourceText = sourceText,
                TargetTextZh = zh,
                TargetTextEs = es,
                TargetTextFr = fr,
                HasBetPlacement = hasBetPlacement,
                Processed = processed
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
            Assert.False(row.Processed); // new rows start on the external pipeline's worklist
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

        // The same English text can legitimately appear under multiple STRING_IDs (a duplicate/
        // legacy row, or two different real-world entities that happen to share a name) -- editing
        // one should propagate to every OTHER row with the identical SourceText, scoped to the same
        // STRING_ID prefix so a team-name edit (Te:) can never bleed into an unrelated market
        // template row (MaTe:) that coincidentally has the same text.
        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_UpdatingOneRow_CascadesToOtherRowsWithIdenticalSourceTextAndSamePrefixOnly()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_UpdatingOneRow_CascadesToOtherRowsWithIdenticalSourceTextAndSamePrefixOnly);
            long editedId;
            using (var seed = BuildTranslationContext(dbName))
            {
                var edited = MakeGlossaryRow("Te:1", "Werewolves CC", zh: "旧翻译");
                var sameKindSibling = MakeGlossaryRow("Te:2", "Werewolves CC", zh: "另一个旧翻译");
                var differentKindSibling = MakeGlossaryRow("MaTe:9", "Werewolves CC", zh: "无关文本");
                seed.TranslationCache2.AddRange(edited, sameKindSibling, differentKindSibling);
                await seed.SaveChangesAsync();
                editedId = edited.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.AddOrUpdateSportsbookGlossary(
                new AddOrUpdateSportsbookGlossaryRequest
                {
                    Id = editedId,
                    StringId = "Te:1",
                    SourceText = "Werewolves CC",
                    TargetLanguage = "ZH",
                    TargetText = "狼人 CC"
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            var editedRow = verify.TranslationCache2.Single(t => t.StringId == "Te:1");
            var sameKindRow = verify.TranslationCache2.Single(t => t.StringId == "Te:2");
            var differentKindRow = verify.TranslationCache2.Single(t => t.StringId == "MaTe:9");

            Assert.Equal("狼人 CC", editedRow.TargetTextZh);
            Assert.Equal("狼人 CC", sameKindRow.TargetTextZh); // cascaded: same prefix, same source text
            Assert.True(sameKindRow.Processed);
            Assert.Equal("无关文本", differentKindRow.TargetTextZh); // untouched: different STRING_ID prefix
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
        public async Task SetSportsbookGlossaryProcessed_UpdatesFlag_AndLogsActivity()
        {
            var dbName = nameof(SetSportsbookGlossaryProcessed_UpdatesFlag_AndLogsActivity);
            long id;
            using (var seed = BuildTranslationContext(dbName))
            {
                var entity = MakeGlossaryRow("SpId:1", "Soccer", processed: false);
                seed.TranslationCache2.Add(entity);
                await seed.SaveChangesAsync();
                id = entity.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.SetSportsbookGlossaryProcessed(
                new SetSportsbookGlossaryProcessedRequest { Id = id, Processed = true },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            Assert.True(verify.TranslationCache2.Single(t => t.Id == id).Processed);

            using var verifyBackoffice = BuildBackofficeContext(dbName);
            var activity = Assert.Single(verifyBackoffice.ActivityLog);
            Assert.Equal(Activity.Update.ToString(), activity.Activity);
            Assert.Equal("SpId:1.Processed", activity.Payload);
            Assert.Equal(bool.FalseString, activity.From);
            Assert.Equal(bool.TrueString, activity.To);
        }

        [Fact]
        public async Task SetSportsbookGlossaryProcessed_NoOpWithoutActivityLog_WhenFlagUnchanged()
        {
            var dbName = nameof(SetSportsbookGlossaryProcessed_NoOpWithoutActivityLog_WhenFlagUnchanged);
            long id;
            using (var seed = BuildTranslationContext(dbName))
            {
                var entity = MakeGlossaryRow("SpId:1", "Soccer", processed: true);
                seed.TranslationCache2.Add(entity);
                await seed.SaveChangesAsync();
                id = entity.Id;
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.SetSportsbookGlossaryProcessed(
                new SetSportsbookGlossaryProcessedRequest { Id = id, Processed = true },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verifyBackoffice = BuildBackofficeContext(dbName);
            Assert.Empty(verifyBackoffice.ActivityLog);
        }

        [Fact]
        public async Task SetSportsbookGlossaryProcessed_ReturnsError_WhenIdNotFound()
        {
            var dbName = nameof(SetSportsbookGlossaryProcessed_ReturnsError_WhenIdNotFound);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.SetSportsbookGlossaryProcessed(
                new SetSportsbookGlossaryProcessedRequest { Id = 999, Processed = false },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);
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

        // ---------------------------------------------------------------
        // ExportSportsbookGlossary / ImportSportsbookGlossary
        // (CSV export/upload-template round trip for the Dynamic Translation screen)
        // ---------------------------------------------------------------

        [Fact]
        public async Task ExportSportsbookGlossary_ReturnsAllRowsWithAllLanguageColumns()
        {
            var dbName = nameof(ExportSportsbookGlossary_ReturnsAllRowsWithAllLanguageColumns);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Basketball", es: "Baloncesto"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球", es: "Fútbol", fr: "Football"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ExportSportsbookGlossary(null);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.Count);
            Assert.Equal("SpId:1", result.Result[0].StringId); // ordered by StringId
            Assert.Equal("足球", result.Result[0].TargetTextZh);
            Assert.Equal("Fútbol", result.Result[0].TargetTextEs);
            Assert.Equal("Football", result.Result[0].TargetTextFr);
            Assert.Equal("SpId:2", result.Result[1].StringId);
        }

        [Fact]
        public async Task ExportSportsbookGlossary_FiltersBySearchText()
        {
            var dbName = nameof(ExportSportsbookGlossary_FiltersBySearchText);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Basketball"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ExportSportsbookGlossary("Soccer");

            var row = Assert.Single(result.Result);
            Assert.Equal("SpId:1", row.StringId);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_CreatesNewRow_WhenStringIdNotFound()
        {
            var dbName = nameof(ImportSportsbookGlossary_CreatesNewRow_WhenStringIdNotFound);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "足球" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.CreatedCount);
            Assert.Equal(0, result.Result.UpdatedCount);

            using var verify = BuildTranslationContext(dbName);
            var row = Assert.Single(verify.TranslationCache2);
            Assert.Equal("Soccer", row.SourceText);
            Assert.Equal("足球", row.TargetTextZh);
            Assert.Equal(string.Empty, row.TargetTextEs);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_SkipsNewRow_WhenSourceTextMissing()
        {
            var dbName = nameof(ImportSportsbookGlossary_SkipsNewRow_WhenSourceTextMissing);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "", TargetTextZh = "足球" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(0, result.Result.CreatedCount);
            Assert.Equal(1, result.Result.SkippedCount);
            Assert.Contains("SpId:1", Assert.Single(result.Result.RowMessages));

            using var verify = BuildTranslationContext(dbName);
            Assert.Empty(verify.TranslationCache2);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_UpdatesExistingRow_WhenValueDiffersFromSourceAndCurrent()
        {
            var dbName = nameof(ImportSportsbookGlossary_UpdatesExistingRow_WhenValueDiffersFromSourceAndCurrent);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "足球" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.UpdatedCount);

            using var verify = BuildTranslationContext(dbName);
            Assert.Equal("足球", verify.TranslationCache2.Single().TargetTextZh);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_SkipsUpdate_WhenValueEqualsSourceText()
        {
            var dbName = nameof(ImportSportsbookGlossary_SkipsUpdate_WhenValueEqualsSourceText);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    // Untranslated placeholder equal to the EN source text -- should be skipped.
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "Soccer" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.UnchangedCount);
            Assert.Equal(0, result.Result.UpdatedCount);

            using var verify = BuildTranslationContext(dbName);
            Assert.Equal(string.Empty, verify.TranslationCache2.Single().TargetTextZh);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_IsIdempotent_ReimportingSameValuesProducesNoChangesOrActivityLogs()
        {
            var dbName = nameof(ImportSportsbookGlossary_IsIdempotent_ReimportingSameValuesProducesNoChangesOrActivityLogs);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var request = new ImportSportsbookGlossaryRequest
            {
                Rows = new List<ImportSportsbookGlossaryRow>
                {
                    new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "足球" }
                }
            };

            var result = await sut.ImportSportsbookGlossary(request, MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(0, result.Result.CreatedCount);
            Assert.Equal(0, result.Result.UpdatedCount);
            Assert.Equal(1, result.Result.UnchangedCount);

            using var verifyBackoffice = BuildBackofficeContext(dbName);
            Assert.Empty(verifyBackoffice.ActivityLog); // re-uploading identical data must not spam the activity log
        }

        [Fact]
        public async Task ImportSportsbookGlossary_LeavesTranslationUnchanged_WhenCellIsBlank()
        {
            var dbName = nameof(ImportSportsbookGlossary_LeavesTranslationUnchanged_WhenCellIsBlank);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        // ZH left blank in the file, ES newly populated.
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "", TargetTextEs = "Fútbol" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.UpdatedCount);

            using var verify = BuildTranslationContext(dbName);
            var row = verify.TranslationCache2.Single();
            Assert.Equal("足球", row.TargetTextZh); // untouched by the blank cell
            Assert.Equal("Fútbol", row.TargetTextEs);
        }

        [Fact]
        public async Task ImportSportsbookGlossary_SkipsDuplicateStringId_WithinSameFile()
        {
            var dbName = nameof(ImportSportsbookGlossary_SkipsDuplicateStringId_WithinSameFile);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(
                new ImportSportsbookGlossaryRequest
                {
                    Rows = new List<ImportSportsbookGlossaryRow>
                    {
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "足球" },
                        new ImportSportsbookGlossaryRow { StringId = "SpId:1", SourceText = "Soccer", TargetTextZh = "Different" }
                    }
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.CreatedCount);
            Assert.Equal(1, result.Result.SkippedCount);

            using var verify = BuildTranslationContext(dbName);
            Assert.Equal("足球", verify.TranslationCache2.Single().TargetTextZh); // first row wins
        }

        [Fact]
        public async Task ImportSportsbookGlossary_ReturnsValidationError_WhenNoRows()
        {
            var dbName = nameof(ImportSportsbookGlossary_ReturnsValidationError_WhenNoRows);
            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ImportSportsbookGlossary(new ImportSportsbookGlossaryRequest(), MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Error, result.StatusCode);
        }
    }
}
