---
name: translation-system
description: How translations work in backoffice-final: Backoffice UI labels, Sportsbook UI labels (Static Translation) and the sportsbook name glossary (Dynamic Translation), the Translation database and the translation change messages. Use when changing the translation screens, language handling or the Translation database.
---

# Translation System

This covers translations in backoffice-final. There are three kinds: Backoffice UI labels, Sportsbook UI labels (the "Static Translation" screen) and the sportsbook name glossary (the "Dynamic Translation" screen). Read it before you change the translation screens, the `Translation` database, language handling, CSV import/export or the translation change messages. All paths are relative to the repo root. Line numbers were checked at commit c57d507d.

## Architecture overview

All translation data lives in the SQL `Translation` database. The code reaches it through `TranslationContext` (`Backoffice.Shared/Data/Translation/TranslationContext.cs`), which is registered with `AddDbContextPool` on `ConnectionStrings:Translation` (`Backoffice.API/Startup.cs:101`). Every read and write happens in `Backoffice.API/Repositories/BackOfficeRepository.cs`. Two controllers expose them:
- `TranslationController`, routed `api/translation`, with actions under `sportsbook/translations...` and `sportsbook/glossary...` (`Backoffice.API/Controllers/Backoffice/TranslationController.cs:25-313`).
- `ConfigController.GetTranslations`, which serves `GET api/config/translations` and is `[AllowAnonymous]` (`ConfigController.cs:346-364`).

```
Backoffice UI labels      BackofficeStatic    read-only here, merged with EN fallback
Sportsbook UI labels      SportsbookStatic    CRUD + CSV import/export, publishes OnStaticTranslationChange
Sportsbook names          TranslationCache2   CRUD + CSV import/export, publishes OnTranslationChange
```

In Backoffice.Web, the screens are `Pages/SBC/StaticTranslation.razor` (`/translations/static`) and `Pages/SBC/DynamicTranslation.razor` (`/translations/dynamic`), each with its own edit and delete modals. The Web project also parses and builds the CSV files (`Services/StaticTranslationCsvHelper.cs`, `Services/GlossaryCsvHelper.cs`), so the API only ever receives structured rows. `TranslationState` fetches the Backoffice UI labels, and its language picker offers only `EN`, `ZH` and `KO` (`Backoffice.Web/States/TranslationState.cs:30`).

The Mongo catalogue also holds translated names in `List<...TranslationMDB>` fields that carry `LaCo`:
- `SpNaTr`, `SegNaTr`, `LeNaTr` and `FiNaTr` on fixtures (`Backoffice.Shared/Data/Sportsbook/Collections/Fixture.cs:45-48`).
- `MaNaTr` on markets.
- `SeNaTr` and `ClBoSeNaTr` on selections.

Backoffice only reads these fields, for example in the EN projections at `TradingRepository.cs:1332,1336`. OE.Translation.Service in odds-engine-2 writes them from the change messages described below (`Backoffice.Shared/OutgoingEngineMessages/OnTranslationChangeMessage.cs:5-10`). `TranslationCache2` is also written from outside this repo: an external pipeline (DeepL) sets `PROCESSED`, and OE.Translation.Service resets it (`TranslationCache2.cs:51-54`).

## TranslationContext entities

| DbSet | Key | Columns |
|---|---|---|
| `BackofficeStatic` | `{LanguageCode, Name}` | `LanguageCode`, `Name`, `Value` (all `string`) |
| `SportsbookStatic` | `{LanguageCode, Name}` | `LanguageCode`, `Name`, `Value` (all `string`) |
| `TranslationCache2` | `Id` (`ID`, `long`, identity), unique index on `StringId` | `STRING_ID` (200), `SOURCE_LANGUAGE` (10), `SOURCE_TEXT`, `TARGET_TEXT_ZH`, `TARGET_TEXT_ES`, `TARGET_TEXT_FR` (250 each), `HAS_BET_PLACEMENT`, `PROCESSED`, `CREATED_AT`, `UPDATED_AT` |

Sources: `TranslationContext.cs:11-27`, and `Backoffice.Shared/Data/Translation/Entities/BackofficeStatic.cs`, `SportsbookStatic.cs` and `TranslationCache2.cs`. The schema authority for `TranslationCache2` is odds-engine-2 (`TranslationCache2.cs:51-54`).

There are no per-entity translation tables. `FixtureTranslation`, `LeagueTranslation` and `MarketTranslation` exist only as DTOs in `Backoffice.Shared/Models/`. For example, `FixtureTranslation` is just `{ LanguageCode, FixtureName }`. They are used inside odds messages such as `OnExternalOddsChangeMessage`. `BackofficeContext` has a `Translation` DbSet, but only as commented-out code (`BackofficeContext.cs:18, 32`).

## Language codes

`LanguageCode` is an enum with uppercase members: `EN = 1`, `TH = 2`, `KO = 5`, `ZH = 6`, `VI = 8`, `ID = 9`, `JA = 11`, `TR = 13`, `PT = 16`, `ES = 18`, `BR = 19` (`Backoffice.Shared/Enums/LanguageCode.cs`). `FR = 15` is commented out of the enum, even though the glossary supports FR. `BR` means Brazilian Portuguese. There are no region-suffixed codes such as `pt-BR`.

The database stores language codes as uppercase strings (`"EN"`, `"ZH"`, `"JA"`, `"BR"`). The code converts with `langCode.ToString().ToUpper()` (`BackOfficeRepository.cs:2566`). A new glossary row gets `SourceLanguage = "EN"` unless the request supplies one (`BackOfficeRepository.cs:3264`).

`ConfigController.GetTranslations` initialises `langCode = LanguageCode.EN` and then calls `Enum.TryParse` on the `x-language-code` header (`ConfigController.cs:352-353`). This is not a real EN fallback. When the parse fails, `TryParse` overwrites `langCode` with `default` (0), and this happens for a missing header, an unknown code, or lowercase `zh`, because the parse is case-sensitive. The request is then served as language `"0"` under the cache key `tr-0`, and it gets EN-only content because no rows match `"0"`.

Each feature supports a different set of target languages:
- Glossary (`TranslationCache2`): only `ZH`, `ES` and `FR` (`IsSupportedTargetLanguage`, `BackOfficeRepository.cs:3619-3620`). The CSV header is `StringId,EN,ZH,ES,FR` (`GlossaryCsvHelper.cs:15`).
- Static (`SportsbookStatic`) import and export: `ZH`, `JA` and `BR` (`BackOfficeRepository.cs:2945`). The CSV header is `Key,EN,ZH,JA,BR` (`StaticTranslationCsvHelper.cs:17`).

## Fallback behaviour

There is no multi-level fallback chain, and there is no `ResolveTranslationAsync`. This is what actually exists:

- **`BackofficeStatic`** (`GetTranslationsByLanguageCode`). The method reads the whole table without caching (`GetTranslations`, `BackOfficeRepository.cs:2479-2483`). It then merges the EN rows with the rows for the requested language, and the requested language wins for each `Name` (`BackOfficeRepository.cs:2564-2607`).

```csharp
// Backoffice.API/Repositories/BackOfficeRepository.cs:2580-2591
var englishTranslations = translations.Where(c => c.LanguageCode == "EN").ToList();
var reqTranslations = translations.Where(c => c.LanguageCode == languageCode).ToList();

var combinedResult = englishTranslations.Concat(reqTranslations)
    .GroupBy(g => g.Name)
    .Select(s => new
    {
        Name = s.Key,
        s.OrderByDescending(x => reqTranslations.Contains(x)).First().Value
    })
    .ToDictionary(s => s.Name, x => x.Value);
```

- **`SportsbookStatic`** (`GetSportsbookTranslations`). The method returns every known key and gives an empty `Value` where the language has no row. There is no EN fallback (`BackOfficeRepository.cs:2609-2652`).
- **Sport names in config screens** (`GetConfigState`). For non-EN requests, `GetSportsLookup` calls the trading API `sports/lookup` endpoint and passes the language in `x-language-code` (`BackOfficeRepository.cs:2389-2397`, `TradingRepository.cs:133-137`). For EN it returns `null`. The caller dereferences `FirstOrDefault(...).SportName` directly (`BackOfficeRepository.cs:1891`). So a sport that is missing from the lookup throws, and so does a lookup call that fails.
- **Mongo lookups** in `Backoffice.API/Repositories/SportsbookRepository.cs` (`GetSportLookup`, `GetLeagueLookup`, `GetSegmentLookup`, `GetMarketLookup`). These methods accept `langCode` but do not use it for translation. The translation lookups are commented out, so they return the raw `SpNa`, `LeNa`, `SegNa` and `MaTeNa` (`SportsbookRepository.cs:1273-1410`).

## Editing Static Translations (SportsbookStatic)

`AddOrUpdateSportsbookTranslation` is an upsert on `{LanguageCode, Name}`. If the row exists, it updates `Value`; otherwise it inserts a new row. It then writes an activity log and calls `PublishStaticTranslationChange` (`BackOfficeRepository.cs:2693-2752`). There is no `TransactionScope`, and a single `SaveChangesAsync` covers the row.

There are two delete operations:
- `DeleteSportsbookTranslation` removes one language row (`BackOfficeRepository.cs:2754-2804`).
- `DeleteSportsbookTranslationKey` removes the key in every language and publishes once for each language it removed (`BackOfficeRepository.cs:2806-2864`). The UI no longer offers an EN view, so this is the only way to retire a key completely.

Import rules (`ImportSportsbookTranslations`, `BackOfficeRepository.cs:2926-3072`):
- A new key needs an EN source text, which is inserted as the `"EN"` row. Without one, the row is skipped. For an existing key, the EN column is ignored, so import never rewrites source text.
- A key that appears twice in the same file is skipped the second time.
- A blank target cell leaves the existing value untouched. A target that equals the source text counts as untranslated and is skipped. A value that equals the stored one is a no-op.
- The import publishes one `OnStaticTranslationChange` for each language it changed.

## Editing the glossary (TranslationCache2)

- `StringId` has these formats: `SpId:{id}`, `SegId:{id}`, `LeId:{id}`, `Te:{teamId}`, `MaTe:{MaTeId}` and `SeTe:{MaTeId}:{SeTeId}` (`OnTranslationChangeMessage.cs:18-19`).
- `AddOrUpdateSportsbookGlossary` refuses an unsupported target language and refuses a duplicate `StringId` on create. On edit, it also overwrites `SourceText` (`BackOfficeRepository.cs:3194-3316`).
- New rows start with `Processed = false`, which puts them on the external pipeline's worklist (`BackOfficeRepository.cs:3270-3272`). `SetSportsbookGlossaryProcessed` toggles the flag and logs the change, but publishes nothing (`BackOfficeRepository.cs:3360-3398`).
- Saving a target-language value also copies it to every other row with the same `SourceText`, whatever its prefix, and sets `Processed = true` on those rows. This is `CascadeToSiblingGlossaryRowsAsync` (`BackOfficeRepository.cs:3318-3358`). Import does not cascade.
- Delete is refused when `HasBetPlacement` is true. Otherwise it publishes an empty `Tx` for every non-empty target language, so downstream caches clear (`BackOfficeRepository.cs:3410-3421`).
- `ImportSportsbookGlossary` follows the same skip rules as the static import (`BackOfficeRepository.cs:3508-3614`).

## CSV parsing (Backoffice.Web)

Both `ParseCsv` methods require an exact header match, ignoring case. BO-98 (c57d507d) changed them to strip trailing blank header cells first, because Excel adds a trailing comma when it saves. The error message now includes a `Found:` list of the headers it read (`StaticTranslationCsvHelper.cs:37-61`, `GlossaryCsvHelper.cs:35-59`). Tests are in `Backoffice.Web.Tests/Services/StaticTranslationCsvHelperTests.cs` and `GlossaryCsvHelperTests.cs`.

## Change messages and caching

After a successful save, a translation edit publishes to the shared exchange. `RabbitMqRepository.Publish` catches publish failures, so a failed publish cannot roll back a save that has already committed (`BackOfficeRepository.cs:3729-3733`).

| Edit | MessageType | Routing key | Payload |
|---|---|---|---|
| Glossary | `OnTranslationChange` | `r.oe.cachesync.ontranslationchange` | `OnTranslationChangeMessage { Tr: [{ StId, LaCo, Tx }] }` |
| Static label | `OnStaticTranslationChange` | `r.oe.cachesync.onstatictranslationchange` | `OnDeleteCacheMessage { CacheType = StaticTranslation, CacheKey = languageCode }` |

Sources: `BackOfficeRepository.cs:3734-3766`. `PublishTranslationChange` sends nothing when the change list is empty.

The consumers are outside this repo. Platform.CacheSync.Service purges the Sportsbook.API caches (`tr` and every `tr-{LANG}` for static labels, whichever language was edited) and relays glossary deltas on `r.oe.ontranslationchange` to odds-engine-2 (`BackOfficeRepository.cs:3748-3752`, `OnTranslationChangeMessage.cs:5-10`).

You can also trigger invalidation by hand with `POST api/config/cachepreload` (`[AllowAnonymous]`, `ConfigController.cs:301-334`). It takes an `OnDeleteCacheMessage` and publishes `OnDynamicTranslationChange` or `OnStaticTranslationChange` on `r.oe.cachesync.{messagetype lowercased}` (`BackOfficeRepository.cs:2545-2554, 3768-3785`).

In-process caches (`IMemCache`, TTL in hours):
- `tr-{LANG}`, 24h: the merged `BackofficeStatic` result (`BackOfficeRepository.cs:2567, 2599`).
- `sp-tr-{LANG}`, 1h: the translated sport lookup (`BackOfficeRepository.cs:2394`).
- `sportLookUp-{langCode}`, 24h: the Mongo sport lookup (`SportsbookRepository.cs:1274, 1302`).

`LANG` is uppercased in the `tr-` and `sp-tr-` keys. No code in this repo deletes these keys, so they expire only by TTL. As a result, a `BackofficeStatic` change made outside this repo can take up to 24h to show. Nothing in this repo writes `BackofficeStatic`.

## Tests

`Backoffice.API.Tests/Translation/TranslationRepositoryTests.cs` covers the SportsbookStatic and glossary methods. It runs against EF Core InMemory `BackofficeContext` and `TranslationContext`, with `IRabbitMqRepository` and `IMemCache` mocked (`TranslationRepositoryTests.cs:25-51`). InMemory does not enforce the SQL Server schema, so these tests never exercise the column length limits or the unique `StringId` index.

The UI is covered by bUnit tests in `Backoffice.Web.Tests/SBC/`: `StaticTranslationTests`, `StaticTranslationEditModalTests`, `DynamicTranslationTests`, `DynamicTranslationEditModalTests` and `DynamicTranslationProcessedModalTests`.
