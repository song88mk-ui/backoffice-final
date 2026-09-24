---
name: sportsbook-domain
description: Domain knowledge for the sportsbook data Backoffice works with: sports, segments, leagues, fixtures, markets and selections, plus the per-client ordering and margin configuration layered on top. Use when working with SportsbookContext, TradingContext, config templates or the Mongo Sportsbook collections.
---

# Sportsbook Domain

Domain knowledge for the sportsbook data Backoffice works with: sports, segments, leagues, fixtures, markets, selections, and the per-client ordering and margin configuration layered on top. Apply this when working with `SportsbookContext`, `TradingContext`, `BackofficeContext` config templates, or the Mongo `Sportsbook` collections.

---

## Entity hierarchy

Each catalogue document carries the ids of all its parents (`SpId`, `SegId`, `LeId`, then `FiId` on markets and `FiId`, `MaId` on selections), so the hierarchy is:

```
Sport (SpId)
└── Segment (SegId)          e.g. a country or region grouping inside a sport
    └── League (LeId)
        └── Fixture (FiId)   FiTyId: FixtureType.Match = 0 | Outright = 1
            └── Market (MaId, template MaTeId)
                └── Selection (SeId, template SeTeId, specifier SeSp)
```

Evidence: `Fixture` has `FiId`, `SpId`, `SegId`, `LeId` (Backoffice.Shared/Data/Sportsbook/Collections/Fixture.cs:14-20); `Market` adds `MaId` (Market.cs:15-20) and `Selection` adds `SeId` (Selection.cs:16-23); `LeagueLookup` is keyed by `SpId`/`SegId`/`LeId` (LeagueLookup.cs:10-12); `ClientLeagueOrder` has key `{ClientId, SportId, SegmentId, LeagueId}` (SportsbookContext.cs:16). `FixtureType` is in Backoffice.Shared/Enums/FixtureType.cs. Segments are a fixed level of the tree, not a separate nested classification. There is no `ParentSegmentId` anywhere and no Segment entity in any DbContext.

Markets and selections are identified by template ids (`MaTeId`, `SeTeId`) that are strings. The `MarketTemplate` enum names a few well-known ones (`FT1X2 = 1`, `FTHDP = 2`, `FTOU = 3`, `HT1X2 = 8`, `HTHDP = 9`, `HTOU = 10`, Backoffice.Shared/Enums/MarketTemplate.cs). Lines of the same market share `MaTeId`+`SeTeId` and differ only by `SeSp` (Backoffice.API.Tests/MarketSettlement/TradingRepositorySettlementTests.cs:200-202).

## Data residency

| Data | Where | Owner in this repo |
|---|---|---|
| Fixture, Market, Selection documents | Mongo `Sportsbook`: `Fixture`, `Market`, `Selection` | Read-only (`TradingRepository`, TradingRepository.cs:52-54) |
| Sport/Segment/League/Market template lookups | Mongo `Sportsbook`: `SportLookup`, `SegmentLookup`, `LeagueLookup`, `MarketLookup` | Read-only (`SportsbookRepository`, SportsbookRepository.cs:56-59) |
| Client sport/segment/league ordering, market template groups and ordering, selector market templates, tier league order, player-props templates | SQL `Sportsbook` via `SportsbookContext` (SportsbookContext.cs:27-37) | Read/write |
| Provider mappings (`SportMapping` ... `SelectionMapping`), clients, per-client league/market config, price boosts, users, audits | SQL `Trading` via `TradingContext` (TradingContext.cs:50-82) | Read/write |
| Config templates, template margins, template assignments, client information, client bet settings, client import batches | SQL `Backoffice` via `BackofficeContext` (BackofficeContext.cs:24-38) | Read/write |
| UI labels and glossary | SQL `Translation` via `TranslationContext` | See translation-system.md |

No code in this repo writes the Mongo catalogue; it is written by odds-engine-2 (for example `OE.Sportsbook.Service`). `SportsbookContext` holds no fixtures, markets or selections. Much of the sportsbook data Backoffice shows comes from HTTP calls to the Sportsbook/OddsEngine APIs rather than from these contexts, for example `GetSports` calls `sports/tree/{fixtureTypeId}` (TradingRepository.cs:73).

Namespace quirk: `SportsbookContext`, its SQL entities and the four Mongo lookup classes are declared as `OE.Shared.Data.Sportsbook[.SQLEntities|.Collections]` (SportsbookContext.cs:5, LeagueLookup.cs:3), while `Fixture`, `Market` and `Selection` in the same folder use `Backoffice.Shared.Data.Sportsbook.Collections`. All of them live in `Backoffice.Shared/Data/Sportsbook/`. `ClientLeagueOrder` is declared in `SQLEntities/ClientSegmentLeagueOrder.cs`.

## Fixture, market and selection status

Statuses are integers on the documents (`FiStId`, `MaStId`, `SeStId`) with a denormalised name (`FiStNa`, `MaStNa`, `SeStNa`). The enums in this repo (Backoffice.Shared/Enums/):

- `FixtureStatus`: `Prematch = 0`, `Live = 1`, `Suspended = 2`, `Ended = 3`, `Closed = 4`, `Cancelled = 5`, `Abandoned = 6`, `Delayed = 7`, `Unknown = 8`, `Postponed = 9`, `Interrupted = 10`, `MatchAboutToStart = 11`, `NotMapped = 99` (FixtureStatus.cs).
- `MarketStatus`: `Cancelled = -4`, `Settled = -3`, `HandedOver = -2`, `Suspended = -1`, `Inactive = 0`, `Active = 1`, `NotMapped = 99` (MarketStatus.cs).
- `Scope`: `Prematch = 0`, `Live = 1` (Scope.cs). Config templates use this as `FixtureStatusId` to split prematch and live settings (Backoffice.Config.Service/Repository/BackofficeRepository.cs:77-79).

Unverified: allowed transitions between statuses. No state machine or transition guard was found in this repo, and no Backoffice code was found that blocks config writes based on fixture status.

## Odds representation

Odds are `decimal Od` on the Mongo `Selection` (Collections/Selection.cs:25) and are passed through unchanged, for example `Odds = selection.Od` in `GetManualMarketSettlementDetails` (TradingRepository.cs:1372). There is no integer scaling. Selections also carry probabilities as `double` (`Pr`, `WoPr`, `LoPr`, `VoPr`, `HaWoPr`, `HaLoPr`) and a `decimal DeHeFa` dead-heat factor (Selection.cs:35-41). `VoidFactor` is an enum `Zero = 0`, `Half = 1`, `One = 2` (Enums/VoidFactor.cs).

## Client margin model

Margins are configured through config templates, not per client directly:

```
ConfigTemplate (Id, Name, SportId, FixtureTypeId, FixtureStatusId, Timestamp)
└── ConfigTemplateMargin  key {ConfigTemplateId, SportId, FixtureTypeId, FixtureStatusId, MarketTemplateId}
        IsActive, Config (JSON string)
ConfigTemplateAssigned (Id, ClientId, ClientCode, SportId, SegmentId, LeagueId, PrematchConfigTemplateId, LiveConfigTemplateId)
ConfigTemplateAssignedMapping  key {ConfigTemplateAssignedId, ConfigTemplateId, FixtureStatusId}
```

(BackofficeContext.cs:15-19; Backoffice.Shared/Data/Backoffice/Entities/ConfigTemplate*.cs.) A client gets one prematch and one live template per league; `-1` means none (Backoffice.Config.Service/Repository/BackofficeRepository.cs:47, 141, 156).

`ConfigTemplateMargin.Config` is a JSON list of `MarketMargin { int T; int M }` (Backoffice.Shared/Models/ConfigSettingsModel.cs:44-48). `T` looks like a time offset in seconds relative to fixture start: the UI prematch buckets are negative, for example `-691200` is "8D" and `-3600` is "1h" (Backoffice.Shared/Utilities/MockData.cs:473-501). `M` is the margin key. Defaults are inconsistent: Config.Service defaults to `M = 101` (BackofficeRepository.cs:36-37), `MockData` gives the "INST" bucket `Key = 101` (MockData.cs:475), but `ModifyLeagueTemplateModal` falls back to `T = -41472000, M = 0` for prematch and `T = 0, M = 0` for live (Backoffice.Web/Components/OddsEngine/Configuration/ModifyLeagueTemplateModal.razor.cs:224-225).

Unverified: the unit and meaning of `M` (the code only ever treats it as an integer key). Also unverified: Config.Service's prematch default uses `T = 691200` (positive, BackofficeRepository.cs:36) while the UI's 8D bucket is `-691200`; whether the consumer cares about the sign is not visible here.

## Segment model

There is no Segment entity with a hierarchy. Segment data appears as:
- `SegId`/`SegNa` on Mongo documents and the `SegmentLookup` collection (`SpId`, `SegId`, `SegNa`, `IsMap`).
- `SegmentMapping` in `TradingContext` for provider mappings (`SegId`, `SegNa`).
- `SegmentId` columns in ordering, config and assignment tables (`ClientSegmentOrder`, `ClientLeagueOrder`, `TierLeagueOrder`, `ClientLeagueConfig`, `ClientMarketConfig`, `ConfigTemplateAssigned`).
- A `Segment` DTO (`SegmentId`, `SegmentName`, `SortOrder`, `Leagues`) in the sports-tree response model (Backoffice.Shared/Models/SportTreeModel.cs:34).

## Config template flow

When template margins are saved, the API writes them with a stored procedure and publishes `UpdateClientMargin`:

```csharp
// Backoffice.API/Repositories/BackOfficeRepository.cs:2100-2108
await _backofficeContext.Database.ExecuteSqlRawAsync("EXEC AddOrUpdateConfigTemplateMarginV2 @ConfigTemplateId, @ConfigTemplateMargins", configTemplateIdParam, parameter);

await _rabbitMqRepository.Publish(new MessageRequest()
{
    CorrelationId = Guid.NewGuid().ToString(),
    MessageType = "UpdateClientMargin",
    RoutingKey = $"r.bo.config.updateclientmargin",
    Payload = JsonSerializer.Serialize(configTemplateId)
});
```

`Backoffice.Config.Service` consumes exactly two message types, `UpdateClientMargin` and `DeleteConfigTemplate` (Backoffice.Config.Service/Worker.cs:58-65). For `UpdateClientMargin` it loads every assignment using the template, builds per-client market configs, and publishes `OnUpdateClientMarketConfig` (`r.oe.web.config.OnUpdateClientMarketConfig`) and `OnUpdateClientLeagueConfig` (`r.oe.web.config.OnUpdateClientLeagueConfig`) for the odds engine (Config.Service/Repository/BackofficeRepository.cs:85-118). `OnUpdateClientMarketConfigMessage` is an output of Config.Service, not an input. `DeleteConfigTemplate` is published on `r.bo.config.deleteconfigtemplate` after the API removes the `ConfigTemplate` row from the context but before `SaveChangesAsync` (BackOfficeRepository.cs:1803-1814). Config.Service then clears the template from assignments inside a `TransactionScope`, setting the side to `-1` or removing the assignment when the other side is unset; margin cleanup is a "to follow" comment (Config.Service/Repository/BackofficeRepository.cs:126-169).

Sort-order changes follow a different path, publishing `OnDeleteCacheMessage` on `r.oe.cachesync.*` (Backoffice.API/Repositories/SportsbookRepository.cs):
- Sport/segment/league order (`AddOrUpdateClientSportSettings`) writes `SportsbookContext` directly and publishes `CacheType.SportSortOrder` on `r.oe.cachesync.onsportsortorderchange` (576-588). It runs `EXEC DeleteClientSportSettings` before, not inside, its `TransactionScope` (470, 476), so a failed insert leaves the client with no ordering.
- Market template groups and market template order are POSTed to the Sportsbook API over HTTP (for example `client/markettemplategroup/post`, 245) and, on success, publish `CacheType.MarketSortOrder` on `r.oe.cachesync.onmarketsortorderchange` (249-261). The copy operations (`CopyClientMarketTemplatesOrder`, 944) write `SportsbookContext` directly instead.

## Key identifiers

| Entity | Type | Notes |
|---|---|---|
| Sport | `int SpId` | `Sport` enum holds provider-style ids, e.g. `Soccer = 10`, `Handball = 99614` (Enums/Sport.cs:8, 21) |
| Segment | `int SegId` | |
| League | `int LeId` | |
| Fixture | `long FiId` in Mongo | `FixtureMapping.FiId` in `TradingContext` is a `string` (Trading/SQLEntities/FixtureMapping.cs:7) |
| Market | `long MaId`; template `string MaTeId` | `ConfigTemplateMargin.MarketTemplateId` is also a `string` |
| Selection | `long SeId`; template `string SeTeId` | |
| Provider ids | `string PrFiId`, `PrMaId`, `PrSeId` | Used by settlement matching |
| Client | `Guid ClientId` (`ClientSportOrder`, `ConfigTemplateAssigned`, Trading `Client`) | Trading `Client` is keyed by `string ClientCode` (TradingContext.cs:17); `ClientInformation.ClientId` is an `int` (Data/Backoffice/Entities/ClientInformation.cs:11); `ClientBetSetting.ClientId` is a `long` (ClientBetSetting.cs:13) |
| Config template | `long Id` | |

Manual settlement receives ids as strings and parses them with `long.Parse` (TradingRepository.cs:1315-1317). Check the declared type on each side before comparing or casting ids across Mongo, `TradingContext` and `BackofficeContext`.
