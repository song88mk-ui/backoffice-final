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
        public async Task GetSportsbookTranslations_ReturnsRequestedLanguageValues_ForKeysThatExist()
        {
            var dbName = nameof(GetSportsbookTranslations_ReturnsRequestedLanguageValues_ForKeysThatExist);
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

        // A key can have a row in one language but no row at all for another (never created) --
        // the UI's "Show only untranslated" filter needs a placeholder entry for it, or that gap
        // is invisible: it never appears in the per-language dictionary the page builds.
        [Fact]
        public async Task GetSportsbookTranslations_ReturnsEmptyPlaceholder_ForKeyMissingRequestedLanguageRow()
        {
            var dbName = nameof(GetSportsbookTranslations_ReturnsEmptyPlaceholder_ForKeyMissingRequestedLanguageRow);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "1st_half", Value = "1st Half" });
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "ZH", Name = "1st_half", Value = "上半场" });
                // "accept" has an EN row but no ZH row at all.
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "accept", Value = "Accept" });
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookTranslations("ZH");

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.Count); // both known keys are present, not just the ones with a ZH row
            Assert.Equal("上半场", result.Result.Single(r => r.Name == "1st_half").Value);
            Assert.Equal(string.Empty, result.Result.Single(r => r.Name == "accept").Value);
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
        // legacy row, or two different real-world entities that happen to share a name -- e.g. live
        // data shows "Argentina" as both a SegId: segment and several Te: team rows, which should
        // all share one translation) -- editing one propagates to every OTHER row with the
        // identical SourceText, regardless of STRING_ID prefix.
        [Fact]
        public async Task AddOrUpdateSportsbookGlossary_UpdatingOneRow_CascadesToEveryOtherRowWithIdenticalSourceText()
        {
            var dbName = nameof(AddOrUpdateSportsbookGlossary_UpdatingOneRow_CascadesToEveryOtherRowWithIdenticalSourceText);
            long editedId;
            using (var seed = BuildTranslationContext(dbName))
            {
                var edited = MakeGlossaryRow("Te:1", "Argentina", zh: "旧翻译");
                var sameKindSibling = MakeGlossaryRow("Te:2", "Argentina", zh: "另一个旧翻译");
                var differentKindSibling = MakeGlossaryRow("SegId:122", "Argentina", zh: "旧翻译3");
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
                    SourceText = "Argentina",
                    TargetLanguage = "ZH",
                    TargetText = "阿根廷"
                },
                MakeRequester("Translations.Dynamic"));

            Assert.Equal(StatusCode.Success, result.StatusCode);

            using var verify = BuildTranslationContext(dbName);
            var editedRow = verify.TranslationCache2.Single(t => t.StringId == "Te:1");
            var sameKindRow = verify.TranslationCache2.Single(t => t.StringId == "Te:2");
            var differentKindRow = verify.TranslationCache2.Single(t => t.StringId == "SegId:122");

            Assert.Equal("阿根廷", editedRow.TargetTextZh);
            Assert.Equal("阿根廷", sameKindRow.TargetTextZh); // cascaded: same prefix, same source text
            Assert.True(sameKindRow.Processed);
            Assert.Equal("阿根廷", differentKindRow.TargetTextZh); // cascaded too: different prefix, same source text
            Assert.True(differentKindRow.Processed);
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
        public async Task ExportSportsbookGlossary_UntranslatedOnly_ReturnsOnlyUntranslatedRowsForLanguage()
        {
            var dbName = nameof(ExportSportsbookGlossary_UntranslatedOnly_ReturnsOnlyUntranslatedRowsForLanguage);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));      // translated
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Basketball", zh: ""));      // blank target -> untranslated
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:3", "Tennis", zh: "Tennis"));    // target equals source -> untranslated
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:4", "Golf", zh: "", es: "Golf esp")); // untranslated for ZH even though ES is done
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ExportSportsbookGlossary(null, "ZH", untranslatedOnly: true);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(new[] { "SpId:2", "SpId:3", "SpId:4" }, result.Result.Select(r => r.StringId).ToArray());
        }

        [Fact]
        public async Task ExportSportsbookGlossary_UntranslatedOnly_ExcludesLetterlessSourceTexts()
        {
            var dbName = nameof(ExportSportsbookGlossary_UntranslatedOnly_ExcludesLetterlessSourceTexts);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:1", "5+", zh: ""));        // letterless -- needs no translation
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:2", "0-1", zh: ""));       // digits + symbol only -- needs no translation
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Over 2.5", zh: "")); // has letters -> genuinely untranslated
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ExportSportsbookGlossary(null, "ZH", untranslatedOnly: true);

            var row = Assert.Single(result.Result);
            Assert.Equal("SpId:1", row.StringId);
        }

        [Fact]
        public async Task ExportSportsbookGlossary_UntranslatedOnly_IgnoredForEnglish()
        {
            var dbName = nameof(ExportSportsbookGlossary_UntranslatedOnly_IgnoredForEnglish);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Basketball", zh: ""));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.ExportSportsbookGlossary(null, "EN", untranslatedOnly: true);

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.Count);
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

        // ---------------------------------------------------------------
        // GetSportsbookTranslationStats / GetSportsbookGlossaryStats / GetSportsbookGlossary(UntranslatedOnly)
        // (translated-vs-untranslated legend on the Static/Dynamic Translation screens)
        // ---------------------------------------------------------------

        [Fact]
        public async Task GetSportsbookTranslationStats_CountsBlankValues_AsUntranslated_PerLanguage()
        {
            var dbName = nameof(GetSportsbookTranslationStats_CountsBlankValues_AsUntranslated_PerLanguage);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "key1", Value = "Hello" });
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "EN", Name = "key2", Value = "World" });
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "ZH", Name = "key1", Value = "你好" });
                seed.SportsbookStatic.Add(new SportsbookStatic { LanguageCode = "ZH", Name = "key2", Value = "" });
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookTranslationStats();

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var en = result.Result.Languages.Single(l => l.LanguageCode == "EN");
            Assert.Equal(2, en.TotalCount);
            Assert.Equal(2, en.TranslatedCount);
            Assert.Equal(0, en.UntranslatedCount);

            var zh = result.Result.Languages.Single(l => l.LanguageCode == "ZH");
            Assert.Equal(2, zh.TotalCount); // total keys is shared across languages (distinct Name)
            Assert.Equal(1, zh.TranslatedCount);
            Assert.Equal(1, zh.UntranslatedCount);
        }

        [Fact]
        public async Task GetSportsbookGlossaryStats_TreatsBlankOrEnEqualValue_AsUntranslated_PerLanguage()
        {
            var dbName = nameof(GetSportsbookGlossaryStats_TreatsBlankOrEnEqualValue_AsUntranslated_PerLanguage);
            using (var seed = BuildTranslationContext(dbName))
            {
                // Fully translated row.
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球", es: "Fútbol", fr: "Football"));
                // ZH blank, ES/FR still equal to the EN source (i.e. not yet translated).
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Tennis", zh: "", es: "Tennis", fr: "Tennis"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossaryStats();

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(3, result.Result.Languages.Count); // ZH, ES, FR -- EN is the source, not applicable

            var zh = result.Result.Languages.Single(l => l.LanguageCode == "ZH");
            Assert.Equal(2, zh.TotalCount);
            Assert.Equal(1, zh.TranslatedCount); // SpId:2's blank ZH cell is untranslated

            var es = result.Result.Languages.Single(l => l.LanguageCode == "ES");
            Assert.Equal(1, es.TranslatedCount); // SpId:2's ES cell still equals its EN source

            var fr = result.Result.Languages.Single(l => l.LanguageCode == "FR");
            Assert.Equal(1, fr.TranslatedCount); // same rule for FR
        }

        [Fact]
        public async Task GetSportsbookGlossary_UntranslatedOnly_ReturnsOnlyBlankOrEnEqualRows_ForRequestedLanguage()
        {
            var dbName = nameof(GetSportsbookGlossary_UntranslatedOnly_ReturnsOnlyBlankOrEnEqualRows_ForRequestedLanguage);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Tennis", zh: ""));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:3", "Golf", zh: "Golf")); // equals EN source -> untranslated
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                TargetLanguage = "ZH",
                UntranslatedOnly = true,
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.MetaData.TotalCount);
            Assert.Equal(new[] { "SpId:2", "SpId:3" }, result.Result.Glossary.Select(g => g.StringId).OrderBy(id => id));
        }

        [Fact]
        public async Task GetSportsbookGlossary_UntranslatedOnly_IsIgnored_ForEnglish()
        {
            var dbName = nameof(GetSportsbookGlossary_UntranslatedOnly_IsIgnored_ForEnglish);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Tennis", zh: ""));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                TargetLanguage = "EN",
                UntranslatedOnly = true,
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.MetaData.TotalCount); // EN has no target column to compare -- filter is a no-op
        }

        [Fact]
        public async Task GetSportsbookGlossary_UntranslatedOnly_ExcludesLetterlessSourceTexts()
        {
            var dbName = nameof(GetSportsbookGlossary_UntranslatedOnly_ExcludesLetterlessSourceTexts);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:1", "5+", zh: ""));        // letterless, blank target -- needs no translation
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:2", "4", zh: "4"));        // letterless, target equals source -- needs no translation
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:3", "0-1", zh: ""));       // digits + symbol only -- needs no translation
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Over 2.5", zh: "")); // has letters -> genuinely untranslated
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:2", "Soccer", zh: "足球")); // has letters, translated
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                TargetLanguage = "ZH",
                UntranslatedOnly = true,
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(1, result.Result.MetaData.TotalCount);
            var only = Assert.Single(result.Result.Glossary);
            Assert.Equal("SpId:1", only.StringId);
        }

        [Fact]
        public async Task GetSportsbookGlossary_WithoutUntranslatedFilter_StillReturnsLetterlessRows()
        {
            var dbName = nameof(GetSportsbookGlossary_WithoutUntranslatedFilter_StillReturnsLetterlessRows);
            using (var seed = BuildTranslationContext(dbName))
            {
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:1", "5+", zh: ""));
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossary(new GetSportsbookGlossaryRequest
            {
                TargetLanguage = "ZH",
                UntranslatedOnly = false,
                MetaData = new MetaData { CurrentPage = 1, PageSize = 10 }
            });

            Assert.Equal(StatusCode.Success, result.StatusCode);
            Assert.Equal(2, result.Result.MetaData.TotalCount); // letterless rows are only excluded from the untranslated view
        }

        [Fact]
        public async Task GetSportsbookGlossaryStats_CountsLetterlessSourceTexts_AsTranslated()
        {
            var dbName = nameof(GetSportsbookGlossaryStats_CountsLetterlessSourceTexts_AsTranslated);
            using (var seed = BuildTranslationContext(dbName))
            {
                // Letterless term, blank in every language -- reads the same everywhere.
                seed.TranslationCache2.Add(MakeGlossaryRow("SeTe:1", "5+"));
                // Lettered term, translated for ZH only.
                seed.TranslationCache2.Add(MakeGlossaryRow("SpId:1", "Soccer", zh: "足球"));
                await seed.SaveChangesAsync();
            }

            using var backofficeCtx = BuildBackofficeContext(dbName);
            using var translationCtx = BuildTranslationContext(dbName);
            var sut = BuildSut(backofficeCtx, translationCtx);

            var result = await sut.GetSportsbookGlossaryStats();

            Assert.Equal(StatusCode.Success, result.StatusCode);

            var zh = result.Result.Languages.Single(l => l.LanguageCode == "ZH");
            Assert.Equal(2, zh.TotalCount);
            Assert.Equal(2, zh.TranslatedCount); // "5+" needs no translation, "Soccer" is translated

            var es = result.Result.Languages.Single(l => l.LanguageCode == "ES");
            Assert.Equal(1, es.TranslatedCount); // only "5+" -- "Soccer" is still blank for ES

            var fr = result.Result.Languages.Single(l => l.LanguageCode == "FR");
            Assert.Equal(1, fr.TranslatedCount); // same rule for FR
        }
    }
}
