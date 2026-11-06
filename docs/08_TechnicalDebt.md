# Technical Debt Analysis

**Document Status:** Detailed Analysis (No Code Changes Made)  
**Date:** June 18, 2026

## Executive Summary

The Backoffice solution has **solid architectural foundations** but exhibits several categories of technical debt that should be addressed systematically. This analysis categorizes issues by severity and provides guidance for remediation.

---

## Price Boost Moved Off the Trading API, and What Is Still Missing (BO-42)

**Date noted:** October 22, 2026

**Issue:** Every Trading controller route is forwarded to the OddsEngine Trading API (`API:TradingAPI:{HK|SA}:Uri`, `http://api.oddsengine.svc:3000` in argocd `bo-api` dev, stg and prd). On DEV and STG that host does not resolve (`Name or service not known`), so Manage Boost, Boosted Settings, `publish` and the fixture lists all failed with HTTP 400 that the Web showed as empty lists or "No Record Found!". argocd has no `api` app under `apps/oddsengine` and nothing in infrastructure provides the service: the Trading API is `OE.API` of the retired `oddsengine` (v1) repo, which is built there as `oe-api` but not deployed. Whether PRD still has it is unknown.

**Resolved (BO-42):** the four price boost routes, the two fixture lists and `publish` no longer call it. `TradingRepository` now reads and writes `PriceBoostConfig` and `BoostedSettings` in the Trading DB (`TradingContext`, registered since before but never used until now), reads fixtures, markets and selections from the sportsbook Mongo, and sends `publish` through `IRabbitMqRepository`. The pure logic is `PriceBoostListBuilder` and `SportsbookFixtureBuilder` (unit tested). The unused `priceboost/audit/list` route was removed. Routes and payloads are unchanged, so the Web is not touched. The fixture lists feed the Add Boosted Odds dialog and the Market Settlement and Message Settlement filters; they leave out what those screens do not read (bet stop, live stream, client margins, market counts) and show English names only. The outright list keeps the league tier order (`TierLeagueOrder`, cached 24 hours under the key `tlo` shared with `SportsbookRepository`). The clients of the price boost list and of Boosted Settings come from the Platform API client list (`TradingRepository.GetPlatformClients`, the same list the Web uses), one source of truth. They were read from `Trading.dbo.Client`, a copy that held only some of the clients (DEV 3 of 5, STG 12 of 62, checked October 23, 2026), so the others showed 0.00 odds and a disabled input in the Add Boosted Odds dialog. The `Client` entity and its `DbSet` were removed from `TradingContext`. The table is retired in flyway in two steps, like the other BO-42 tables: renamed to `Client_DELETE_AFTER_BO42` once this change runs in the environment (rollback: rename it back), dropped in a later migration after a full release.

**Resolved (BO-141):** `GET api/trading/client` answers from the Platform client list (`IBackofficeRepository.GetPlatformClients`), filtered to the caller's client codes (`ALL` keeps every client, a caller without client codes gets none). Its action had been commented out and its `[HttpGet("client")]` attribute sat on `GetClientConfig`, so the route returned HTTP 415 on DEV and STG and the Config page's client filter was always empty ("No Clients Available"). The same route feeds `ViewAllLogsModal` and the margin import in `ExcelService`, which failed for the same reason. All clients are listed, active or not, like the other Platform-list callers. A test guards that `GetClientConfig` stays POST only (`TradingControllerClientListTests`).

**Resolved (BO-143):** the OE Config page no longer hides a failed call. In the API, `GetTemplateConfigCount` and the Unassigned leagues list (`GetConfigState`) return an Error response when the league lookup or the Platform client list fails, where they used to return a count of -1 or an empty list as a Success (`ConfigCountTests`). In the Web, a count that failed shows "(Error)" on its tab instead of "Calculating...", a count of 0 shows "(0)", and `Configuration2`, `ConfigAssigned` and `ConfigMyTemplates` show a plain error alert (the API message goes to the log, not the screen). `ConfigNavMenuParamModel.CountLoading` (-1) and `CountFailed` (-2) name the two states (`ConfigLoadFailureTests`). Not fixed here: `Configuration2.Init()` clears its preloader without a render of its own, so the page only leaves the spinner if a count call finishes after the sports and client calls (always true over HTTP in practice, but the test has to delay the first count to reproduce it); the Assigned and Unassigned counts ignore the client filter (`query.Where` result unused); `GetAssignedCount` is dead code.

**Resolved (BO-144):** the Unassigned leagues list and count of the OE Config page no longer call the old Odds Engine API (`league/lookup`). `TradingRepository.GetAllLeagueLookup` now reads the sportsbook Mongo (`LeagueLookup`, with the sport and segment names from `SportLookup` and `SegmentLookup`, shaped by `LeagueLookupBuilder`) and keeps its signature, so its callers did not change. A league is unassigned for a client when the client has no row for it in `Trading.dbo.ClientLeagueConfig` (new `GetClientLeagueConfigRows`); a restricted row (`IsActive = 0`) and an allowed row both count as assigned. The old template assignments (`ConfigTemplateAssigned`) no longer take leagues out of the Unassigned list or count. A failed lookup or settings read is an Error (BO-143), not an empty list. The Unassigned count now covers only the caller's clients and ignores a saved setting for a league that is no longer in the lookup, and the dead `GetAssignedCount` was removed (`ConfigCountTests`, `LeagueLookupBuilderTests`, `TradingRepositoryLeagueConfigTests`). Not changed here: the Assigned list and count still read `ConfigTemplateAssigned`, so the Assigned count still ignores the client filter, until the change that saves restrictions moves them to `ClientLeagueConfig`; and `GetConfigState` empties the client list when the caller has exactly one client code that is not ALL, so a single-client user would see no leagues (found by reading the code, not checked in a browser).

**Resolved (BO-142):** the Config template editor and the Config View Logs filter (`ViewLogsFilter`, opened from `HeaderConfigTopMenu`, which no page renders at the moment, so it could only be checked by the unit tests; BO-155 removed it) no longer call the old Odds Engine API for their market groups and market list (`markettemplategroups`, `marketlookup`). On DEV and STG both calls failed, so the editor showed "No Markets Available" and the Logs filter threw on the empty result. Two new read-only routes answer from the Sportsbook data, `GET api/config/market-groups` and `GET api/config/markets/{fixtureTypeId}/{sportId}` (`ConfigController`, `IBackofficeRepository.GetConfigMarketGroups` and `GetConfigMarketLookup`). The groups come from the SQL table `Sportsbook.dbo.MarketTemplateGroup2` (`SportsbookRepository.GetMarketTemplateGroups`: `FixtureStatusId` 1 is Live and 0 is Prematch, and the ids column is split on commas and trimmed). The list comes from the Mongo `MarketLookup` collection (`SportsbookRepository.GetMarketLookup`, now on `ISportsbookRepository`). The `MarketTemplateGroup` entity was mapped to a table of the same name that is empty on DEV and STG, so `SportsbookContext` now maps it to `MarketTemplateGroup2`, as platform-final does. `MarketTemplateGroup2` has groups for sport 4 only (8 rows on DEV and STG), so every other sport gets the single "All Markets" group the editor already falls back to. The editor shows an error alert when either call fails, instead of "No Markets Available" (`ConfigMarketListLoadingTests`, `ConfigMarketListTests`). Not changed here: the routes `api/trading/markettemplategroups` and `api/trading/marketlookup` still forward to the Trading API, so the Monitoring pages (`CloseMarketsModal`, `MarketGroupFilter`) were still broken on DEV and STG at that point (BO-154 below moved them to the new route). Not known: what the old route returned for its scope argument (the Config pages merge the Live and Prematch lists, so both are returned), and whether PRD still has the Trading API; if it does, these pages now show the Sportsbook data there, which has groups for sport 4 only. The template editor's margins are view-only until margin per client is enabled, because the odds come from the provider by default: `ConfigEditTemplate.MarginsLocked` is always true, which makes only the key and time nodes non-clickable (`AccordionTemplateConfiguration.IsTimelineReadOnly`: no Edit Config popover, no "Click to edit key" tooltip, default cursor). The market Active/Inactive toggle, ACTIVATE ALL and DEACTIVATE ALL, the template name, Cancel and Save still work, and a market saves with its default margins (one key of 101 at kick off). The "Please update margin for Active markets. 101 by default is not allowed." check on Save is skipped while `MarginsLocked` is true, because the default is the only value a locked template can have, and it returns when the flag is turned off. Copy Template still copies the margins as they are (`ConfigEditTemplateReadOnlyTests`). To allow editing again, make `MarginsLocked` follow `View` instead of returning true.

**Resolved (BO-151):** the OE Config page assigns leagues to a client as Active or Restricted, changes that state, and unassigns them, with no templates or margins. `ConfigController` `template/set/{state}` now calls `BackOfficeRepository.SetConfigState`, which saves one `Trading.dbo.ClientLeagueConfig` row per client and league through `TradingRepository.UpsertClientLeagueConfigRows` / `DeleteClientLeagueConfigRows` (the table key makes a second assignment update the row), instead of `ConfigTemplateAssigned`. `ConfigSetLeagueStateRequest.Restricted` defaults to false (Active), so an older Web build that does not send it can never restrict a league by accident. The change is refused as a whole for a league of a client outside the caller's client codes (the route had no such check), and one activity log row per changed league records the user, the old and the new state (`Create`, `Update`, `Delete`; a failed log write is reported but does not undo the save). The Assigned list (`BuildAssignedLeagues`) and the Assigned count now read `ClientLeagueConfig` too, with the league names from the sportsbook lookup (a saved league missing from the lookup is still listed so it can be unassigned), and `ConfigClientModel.IsRestricted` carries the state; the Assigned count now respects the client filter. `GetConfigState` no longer touches the `ConfigTemplateAssigned` table unless a template id is given (the template dialog "View Assigned"): that table did not exist on DEV at the time, and the BO-144 Unassigned list still queried it there and failed (the tests used an in-memory table that always exists, `UnassignedAndAssignedLists_DoNotTouchTheTemplateTables` now guards it). The Web has one new dialog, `AssignLeaguesModal` (nothing pre-selected, the user must choose Active or Restricted), the page toolbar offers Assign, Change state and Unassign, and the rows show a State column instead of the two template columns (`AssignLeaguesModalTests`, `ConfigAssignedTests`, `ConfigSetStateTests`, `ConfigCountTests`, `TradingRepositoryLeagueConfigTests`). Not changed here: nothing reads the saved state yet, so a Restricted league is still visible to the client until the Odds Engine, the site, live odds and bet validation use it (the follow-up tickets); `ModifyLeagueTemplateModal` is no longer opened by the page but stays in the project; the My Templates tab and Copy Client Settings still depend on the template tables (created on DEV by the infrastructure flyway script V22 and applied there by hand; not applied on STG or PRD yet); the new labels ("Assign", "Unassign", "Change state", "Active", "Restricted", "Not reviewed", "State" and the dialog texts) have no translation rows, so they show in English; two users assigning the same league at the same moment can make one of the saves fail on the table key, and the dialog then shows its plain error so the user can try again. The first run on DEV found that `Trading.dbo.ClientLeagueConfig.IsActive` is a `smallint` column (0 or 1, NULL allowed) while the entity exposes a `bool`: the first assign saved correctly, but every later read of a saved row failed with `Unable to cast object of type 'System.Int16' to type 'System.Boolean'`, so the Assigned and Unassigned lists and counts, changing the state and unassigning all returned an error (the in-memory tests never check column types, and DEV had no rows before). `TradingContext` now maps `IsActive` with `HasConversion<short>()` (`ClientLeagueConfigModelTests` checks the mapping, and the table was read and written through the real SQL Server column in a check that was rolled back). A row whose `IsActive` is NULL still fails to read (`SqlNullValueException`); STG was read on 2026-11-02 (the same `smallint` column, and the table has 0 rows, so no NULL); PRD was not read, so run `SELECT COUNT(*) FROM Trading.dbo.ClientLeagueConfig WHERE IsActive IS NULL` on it before release. The activity log rows of these changes have no user name on DEV, because the Back Office login token has no `UserName` claim and `ConfigController` reads that claim (the user id is saved); not changed. My Templates opens on "All Scopes" instead of Live (the scope drop-down then sends no `scopeId`, which the API reads as every scope), because a template saved in Prematch showed "No Record Found" under the Live default (`ConfigMyTemplatesScopeTests`). `NoRecordFound` shows the English sentence when `no_record_message` has no translation row (`NoRecordFoundTests`). DEV has no rows in `Translation.dbo.BackofficeStatic` (STG has 793: EN 12, KO 369, ZH 412), so `no_record_message`, `success_save_template_message_{&}` and `success_remove_template_message` showed as raw keys there; the infrastructure script `R__Seed_080_Translation_BackofficeStatic.sql` inserts the missing STG rows on DEV only (insert only, an edited text is never overwritten). After a save the Assigned or Unassigned list could stay on its spinner over a loaded list: `ConfigAssigned.HandleModalSave` called `StateHasChanged()` after awaiting the JS call that hides the selection bar, but the page only draws while `_shouldRender` is set, and `OnAfterRender` clears it (in Blazor Server it runs once the browser has confirmed the earlier draw, which can land during that call), so the final draw was skipped. It now sets `_shouldRender = true` right before `StateHasChanged()` (`ConfigAssignedTests`). The root cause was found from the DEV logs (every request answered 200 in under 200 ms and bo-web logged no error) and the code, and the fix is checked by a test that clears the flag during the JS call; it was not reproduced in a browser.

**Resolved (BO-154):** the Monitoring market group filter (`MarketGroupFilter`, on the Monitoring detail page) and the Close Markets dialog (`CloseMarketsModal`) no longer call the old Odds Engine API for their market groups. On DEV and STG that call (`GET api/trading/markettemplategroups/99`) answered HTTP 400 ("Name or service not known (api.oddsengine.svc:3000)"), and both components then showed no groups and no message. They now call `BackOfficeService.GetConfigMarketTemplateGroups` (`GET api/config/market-groups`, the Sportsbook data added in BO-142), pick the groups of the fixture's sport as before, and show an error alert when the load fails. A sport with no group rows (every sport except 4 on DEV and STG) gets one "All Markets" group holding every market of the fixture, as the Config editor does. `MarketGroupFilter` also no longer calls its callback with null when no group matches the fixture's markets: `MonitoringDetails.OnMarketTemplateGroupClickEvent` reads `model.MarketTemplateGroup` and would have thrown a NullReferenceException, which the failing route had been hiding (`MonitoringMarketGroupsTests`). Not changed here: `GET api/trading/markettemplategroups/{fixtureTypeId}`, `TradingRepository.GetMarketTemplateGroups` and the web methods `BackOfficeService.GetMarketTemplateGroups` and `TradingService.GetMarketTemplateGroups` now have no caller in the Web project and can be removed in a cleanup. Not checked: the two components in a browser on DEV or STG, and PRD (whether it still has the Trading API, and which sports have group rows in its `MarketTemplateGroup2`).

**Resolved (BO-155):** Config > Assigned and Unassigned has a Logs button (`ConfigAssigned.HandleLogsClick`, both tabs, read only, so also for a user who cannot edit) that opens `ViewLeagueLogs`, which lists the league changes from the activity log (details at the end of this paragraph). The old Config "View Logs" dialog (`ViewLogs`) and its filter (`ViewLogsFilter`) are removed, together with their two models (`ConfigLogsFilterModel`, `ConfigLogsDetailsModel`), the "Logs" link of `HeaderConfigTopMenu` and its two unit tests in `ConfigMarketListLoadingTests`. Nothing could open the dialog: `HeaderConfigTopMenu` is rendered by no page, and the Logs button on My Templates opens `ViewMyTemplatesLogs`, a different dialog that stays. The old dialog was removed instead of put back on a page because its only data call, `TradingService.GetConfigLogs` (`POST api/trading/client/config/audit`, forwarded by `TradingRepository.GetConfigLogs` through `GetHttpClient`), goes to the old Odds Engine Trading API, which answers HTTP 400 on DEV and STG (BO-142, BO-154), so it would open and show nothing there. `HeaderConfigTopMenu` itself stays, with its Import/Export Margins link, because the margin import and export is expected to come back when margins are editable again (see BO-142, `MarginsLocked`); it is still rendered by no page. Not changed here: `ITradingService.GetConfigLogs`, `TradingController.GetConfigLogs` and `TradingRepository.GetConfigLogs` (the route above) now have no caller in the Web project and can be removed in a cleanup, and `TemplatesViewLogs.razor` is an unreferenced placeholder (`<h3>TemplatesViewLogs</h3>`) that was not part of this ticket. How the new dialog works: `SetConfigState` (BO-151) writes one activity log row per league change, with the payload `{clientCode}.{sportId}.{segmentId}.{leagueId}`, the old and new state in From and To (`Unassigned`, `Active` or `Restricted`) and the activity `Create` (assigned), `Update` (state changed) or `Delete` (unassigned). BO-155 made the page of those rows a constant, `SBC.OE.Config.Leagues` (`BackofficeRepository.ConfigLeagueActivityLogPage`, the same value as `ViewLeagueLogs.LogPage`, each pinned by a test). Before, the page was the `x-page` header of the caller, or `Config` when it was empty, and `BaseService.Page` stays set for the whole session once a page sets it (`DeleteTemplateModal`, `DynamicTranslation` and `StaticTranslation` set it and never reset it), so a league row could land on `SBC.OE.Config`, among the template rows, or under a translation page (a test shows it). Read from the code, not reproduced: on `SBC.OE.Config` it would have made `ViewMyTemplatesLogs` throw, because that dialog parses the To of a `Create` row as a template. The dialog reads the page through `POST api/config/activitylog/list` (the same `GetActivityLogs` stored procedure as every View Logs dialog, so it needs `SBC.OE.Config` View and the procedure on that database) with a filter on client, sport, dates and text, 20 rows a page. The row holds ids only, so the league name comes from the league lookup (`GetLeagueLookup`, one call per sport and segment on the page); an unknown league shows as `League {id}`, and a failed read shows an error, not "No Record Found". Known limits: only rows written after BO-155 are on that page, so the history before it is not shown (a one-off script that retags the old rows was considered and not done); the activity log is not filtered by the caller's clients, like every other View Logs dialog, so a user limited to some clients can read the rows of the others; `BaseService.Page` is still never reset, which other writers of the activity log can still be affected by; and the rows `SetConfigState` writes have a user id but no user name, because `AuthorizeAttribute` fills `HttpContext.Items["UserName"]` from a `UserName` claim and the Auth tokens carry `TokenName` instead (`Auth.Api/Repositories/AuthRepository.cs:91`; the Web app reads `TokenName`, `AuthenticationService.cs:143`). The DEV rows written by BO-151 show it (`UserName` is NULL, `UserId` is set), the dialog shows a dash for them, and it affects every API-written log that takes its author from that item (12 reads in `ConfigController`, `TranslationController` and `ClientImportController`). Falling back to `TokenName` in `AuthorizeAttribute` would fix them all, and was left out of BO-155 because it changes the author of the logs of those other controllers.

**Still open:**
- About 50 other Trading routes still forward to the Trading API (client config, mapping, settlement lists, notifications, market lookups, bets).
- Nothing deployed applies a boost or watches the cap. `odds-engine-2` has no code that reads `PriceBoostConfig` or `BoostedSettings` or sets the Mongo `IsBo`/`ClBo` flags the player site reads; that lived in the v1 adapters and in `OE.BoostedOddsCap` (cap threshold notification and email), none of which is deployed. A cap job is planned in `odds-engine-2`.
- `GetPriceBoostConfig` deletes the boosts of fixtures that are no longer in Mongo (a write on a read, as the Trading API did). In an environment whose Mongo does not hold the fixtures, such as DEV, listing deletes every boost.
- `Trading.dbo.PriceBoostConfig` has no primary key and the EF model keys it on the market (client, sport, segment, league, fixture type, fixture, market template, market specifier), not the selection. A different selection of the same market now always replaces the saved one; the Trading API replaced only when both the template and the specifier differed, which could leave two selections in one market. A row whose `MarketSpecifier` is NULL cannot be updated or deleted through EF.
- The Mongo reads of `GetPriceBoostConfig`, `GetFixtures` and `GetOutrightFixtures` sit behind the static Mongo init of `TradingRepository` (see "MongoDB Static Connection" below) and have no unit test; the SQL, the merge rules and the shaping rules are covered, and the Mongo filters and projections were checked to render under the driver.
- `GetFixtures` (match fixtures) no longer requires a client code on the fixture. The Trading API listed only fixtures whose Mongo `ClCo` was non-empty (offered to at least one client), but nothing in the v2 pipeline stamps `ClCo` (bet-genius publishes it as an empty string and `odds-engine-2` has no league config consumer), so on DEV (checked October 22, 2026) all 8,994 fixtures had `ClCo` empty and the Add Boosted Odds, Market Settlement and Message Settlement pickers listed no match. The condition is left out of `SportsbookFixtureBuilder.MatchFixturePredicate`; once `ClCo` is populated again, add `&& !string.IsNullOrEmpty(c.ClCo)` there and update the "Match fixture filter" tests in `SportsbookFixtureBuilderTests`: they build fixtures without a client code, so all four fail until the helper stamps one and the first test asserts the old rule instead. `GetOutrightFixtures` never had the condition.
- A market without client margins now gives every client the provider odds as its current (client) odds, in `PriceBoostListBuilder.CalculateClientOdds`. The Trading API gave such a market no client odds, and the Add Boosted Odds dialog disables the boosted odds input of a client whose current odds are 0, so no boost could be entered and Save stayed disabled. Nothing in the v2 pipeline stamps `ClCoMa` (on DEV, checked October 23, 2026, none of the 365,048 markets had it). A market that is stamped but does not name a client still gives that client no odds. To require the margins again, return the empty result at the top of that method and remove the three "provider odds" tests in `PriceBoostListBuilderTests`.
- Price boost activity logs are written by the Web only. The Trading API also published them to `r.oe.util.logs`, which nothing consumes.

---

## CSV Client Update Tool — Silent Data Corruption on Blank Cells and Currency Edits (BO-112)

**Date noted:** October 8, 2026 (fixed same date, found during code review before merge); second review round October 9, 2026 (see below)

**Issue:** Three bugs in `ClientImportRepository.CommitUpdateClient` (the bulk CSV update flow) could silently overwrite real production client data:
1. `BuildBetSettingFromRow` (`Backoffice.API/Repositories/ClientImportRepository.cs`) built the Cricket/Priceboost/BetBuilder bet-limit categories from the row's Soccer fields unconditionally — the update CSV has no columns for those three categories. Editing any field on an existing currency (even just a Parlay limit) replaced that currency's real Cricket/Priceboost/BetBuilder values with copies of its Soccer numbers, and `SummarizeBetSetting` didn't include those three categories in the audit diff, so the corruption was invisible in both the Confirm screen and the change history.
2. Region/Email used `first.Region ?? existingClient.Region`, but `ClientUpdateCsvParser.Get()` never returns null for a blank cell (only `""`), so the `??` never triggered — every blank Region/Email cell wiped the existing value to empty string.
3. MonthlyFee/RevenueShareSportsbook/RevenueShareJackpot had no blank-preserves-existing guard at all (`ClientUpdateRowRequest` had them as non-nullable `decimal`, defaulting blank cells to `0`), so leaving any of them blank — which every other optional column in this CSV allows — zeroed real financial data on commit.

Also found in the same review: the plain Edit Client modal (`CreateOrUpdateModal.razor.cs`) never populated or forwarded `LimitId`/`Endpoint` when saving, and `BackOfficeRepository.GetClientList` (the query behind the main Clients page) never read those fields from Platform in the first place — so saving any client through the ordinary Edit screen silently cleared LimitId/Endpoint that had been set via CSV import/update.

**Resolved:**
- `BuildBetSettingFromRow` now takes the currency's existing `BetSetting` and carries Cricket/Priceboost/BetBuilder over untouched when the currency already exists; it only defaults them from Soccer when adding a brand-new currency with no prior values to preserve. `SummarizeBetSetting` now includes all six categories so any future divergence shows up in the diff.
- `ClientUpdateRowRequest.MonthlyFee`/`RevenueShareSportsbook`/`RevenueShareJackpot` are now `decimal?`; the parser returns `null` for a blank cell via a new `GetNullableDecimal`, and the repository resolves with `first.MonthlyFee ?? existingClient.MonthlyFee` (same pattern now used for Region/Email, switched from the no-op `??` to an explicit `IsNullOrWhiteSpace` check matching how Name/Website already worked).
- `GetClientList` now fetches `GetPlatformClients()` once per page and maps `LimitId`/`Endpoint` onto each `ClientListResponse.Client`; `CreateOrUpdateModal.razor.cs`'s save request now forwards `Client.LimitId`/`Client.Endpoint` instead of dropping them.
- Also fixed in the same pass (perf, not correctness): `CommitUpdateClient` and `ExportClients` each re-fetched the full Platform client list and ran one `GetBetSetting` DB query per client in an N-client batch. Added `IBackofficeRepository.GetBetSettingsBulk(List<int>)` (one query, grouped in memory) and hoisted the Platform client fetch out of the per-client loop in both call sites.
- Covered by `Backoffice.API.Tests/ClientImport/ClientImportRepositoryUpdateTests.cs` (blank-fields-preserve-existing-values, editing-an-existing-currency-preserves-the-three-uncovered-categories, adding-a-brand-new-currency-still-defaults-from-Soccer).

**Second review round (October 9, 2026, PR #218) — further bugs found and fixed:**
- **TaxConfig and TelephoneNumber wiped on every CSV update.** `CommitUpdateClient` built a fresh `ClientListResponse.Client` without either field; its constructor defaults `TaxConfig` to zeros, and `AddOrUpdateClient` writes both to `ClientInformation` and pushes the zeroed tax config to Platform's bet config. Now carried over from `existingClient` (TaxConfig deserialized as stored — fractions, which is what `AddOrUpdateClient` expects; only `GetClientList` scales to percent for display).
- **An unedited export couldn't be re-uploaded.** `ClientCsvHelper` writes a UTF-8 BOM and the modals decode with `Encoding.UTF8.GetString`, which keeps it, so the first header became `﻿ClientCode`. Both `ClientUpdateCsvParser` and `ClientImportCsvParser` now strip a leading BOM.
- **Lowercase client codes validated "Ready" then failed at commit** — validation compared case-insensitively, the commit dictionary didn't. Commit lookup is now `OrdinalIgnoreCase`.
- **Blank bet-limit cells became `0`.** All ten bet columns are now `decimal?`; a non-Remove currency row with any blank bet column is a validation error naming the blank columns.
- **Commit trusted the caller to have validated.** `CommitUpdateBatch` now re-runs the same per-client validation (`GetUpdateGroupError`) and fails rows that don't pass — e.g. `CurrencyAction = "Delete"` was previously treated as an upsert.
- **Platform failures were invisible.** If `GetPlatformClients()` fails, the batch is now aborted before anything is written; if a client isn't in the Platform list, its row fails instead of sending LimitId/Endpoint as empty.
- **Audit write failure misreported the row.** The row is now marked Succeeded before the `ActivityLog2` write (the client is already updated by then); a failed audit write is logged and noted in `StepsCompleted`. `ErrorMessage` is truncated to its 1000-char column.
- **Dashboard slowdown.** The Platform fetch added to `GetClientList` is now opt-in via `GetClientListRequest.IncludePlatformFields`; only the Clients page sets it, so both Dashboards' 25-per-page loops no longer trigger a full Platform fetch per page.
- **History modal hid errors** — a failed load now shows an error instead of "No update batches found".
- Tests: `ClientImportRepositoryUpdateTests.cs` (now 10) and `Backoffice.Web.Tests/SBC/ClientCsvRoundTripTests.cs` (export → re-import round trip, blank bet cell → null, BOM on the Create parser).

**Third review round (October 12, 2026) — fixed:**
- **A failed bet-settings read could delete all of a client's currencies.** `CommitUpdateBatch` treated a failed `GetBetSettingsBulk` (e.g. one malformed JSON row) as "no existing settings", and `AddOrUpdateClient` replaces *all* of a client's bet settings with the list it's given — so even a Website-only change would delete every currency and push an incomplete bet config to Platform. The batch now aborts before anything is written if that read fails. `ExportClients` fails the same way instead of exporting clients with no currencies.
- **A failed Platform update was reported as Succeeded.** `AddOrUpdateClient` only logs Platform failures and still returns Success; it sets `PlatformClientId` only when Platform accepts the update. The update flow now checks that (as the Create flow already did) and marks the row Failed — "Saved in Backoffice, but the Platform update failed" — while still writing the audit entries, since the Backoffice values did change.
- **A batch could be stuck Running and lock the environment.** Reads ran after the batch was saved as Running but before the `try/finally` that closes it; an exception there left the lock with no automatic recovery (a manual SQL fix, a production write on PRD). All reads now happen before the batch is created.

**CSV Create didn't set up Sport Settings (reported October 12, 2026).** The Create import copied only `ClientSelectorMarketTemplate2` from the reference client, so a new client had no `Sportsbook..ClientSportOrder` rows (no sports listed), nor `ClientSegmentOrder`/`ClientLeagueOrder`. It now also copies those three tables from the reference client — the set the Sport Settings screen saves as one unit — and the row result says so, or says the reference client had none to copy. Clients imported before this fix need the one-time backfill script (run manually; it's a dry run by default). The copy is raw SQL, which the EF InMemory test provider can't execute, so like the existing market-template copy it has no unit test; its column lists were checked against the `V1__Baseline` schema. Other per-client Sportsbook tables (`ClientSegmentLeagueOrder`, `ClientMarketTemplateGroup/Order`, `ClientTopLeague`) are still not copied; `ClientTopFixture` deliberately isn't (fixture-specific).

**Remaining in `AddOrUpdateClient` (shared method, not changed here):** the 3× `ProcessBackofficeClientBetConfig` retry loop gives up silently after the last attempt — Platform can end up with stale bet limits while the method returns Success and `PlatformClientId` is set, so the check above can't catch it. Fixing it means making `AddOrUpdateClient` report Platform/bet-config failures, which every caller (Edit Client, CSV create, CSV update) would need to handle.

**Dev was blocked as "unknown environment" (October 13, 2026).** Environment detection matches the Backoffice connection string's host. Dev's SQL Server runs in-cluster (Helm release `mssql`, chart `mssqlserver-2022`): the dev API reaches it by the service name `mssql-mssqlserver-2022`, while outside clients use the `k8s-dev-database-….elb` load balancer — only the latter was in the list. Both now map to Dev (confirmed against the dev server: `@@SERVERNAME` is `mssql-mssqlserver-2022-…` and the `bo-api` pod is a connected client). The blocked message now names the unrecognised host (server only, never credentials).

**Client ID (October 12, 2026):** the story's "Client ID" is the existing Platform client ID (`Platform.dbo.Client.ClientId`), not a new column — it was already unique, required, and used by every other system, and a second typed-in ID with the same name would have collided with it. The update CSV now has a required `ClientId` first column; rows are matched by it (via `ClientIdRef`), and the row's Client Code must belong to that client as a guard against a mis-copied ID. Export writes the column; the single downloadable template (`client-import-template.csv`, shared by Import and Update — each parser ignores the other flow's columns) includes it; the Client Information modal shows it read-only. **Behaviour change:** update files exported before this change (no `ClientId` column) are rejected with a message to re-export. The Create CSV is unchanged — Platform assigns the ID on create (superseded by GM-1001 below).

**Chosen Client ID on create, and CreatedDate (October 26, 2026, GM-1001):** the Create CSV may now fill `ClientId` to create the client with that ID; blank still means Platform generates one, and files without the column still parse. The parser rejects a value that isn't a GUID (or is all zeros). `ValidateBatch` rejects an ID that already belongs to a client, and when that client's LimitId differs from the row's it names both ("already belongs to client "OKT" with LimitId 10939, not 2535"). It also rejects different IDs for one client's currency rows and the same ID for two clients in one file. It checks Platform's client list and Backoffice's own `PlatformClientId`s, because Platform's list is cached for 24h and can miss a client created minutes ago. `CommitClient` re-runs the check, then passes the ID through as `ClientListResponse.Client.RequestedPlatformClientId`. That is a separate field from the output `PlatformClientId`, which `AddOrUpdateClient` sets only once Platform succeeds. `AddOrUpdateClient` then sends it to Platform as `ClientRequest.ClientId`. Platform (platform-final `PlatformRepository.AddOrUpdateClient`) uses it only when inserting, rejects one that already exists, and never changes an existing client's ClientId. On insert it also stamps `Client.CreatedDate` (UTC), which was previously always NULL. Existing rows stay NULL; there is no backfill. **Deploy order:** deploy platform-final first. An older Platform ignores the requested ID and generates its own; `CommitClient` detects that, fails the row ("registered the client as X instead of the requested Client ID Y") and stops before copying templates and sport settings. The Backoffice and Platform records will already exist under the generated ID and need manual follow-up. Tests: `ClientImportRepositoryCreateClientIdTests.cs`, the `CreateParser_*` cases in `ClientCsvRoundTripTests.cs`, and platform-final `AddOrUpdateClientTests.cs`.

**Imported clients crashed the sportsbook until its pods restarted (October 27, 2026).** Found on staging with client SOL. The import succeeded, but every page load on sb.stg failed with `NullReferenceException` at platform-web `Index.razor.cs:458` (`SportsTreeList.FirstOrDefault().SportId`), because the client's sports list came back empty. Sportsbook.API caches the whole `ClientSportOrder` table for 24h (`"so"`; also `seo`/`leo` for segments and leagues, `csmt`/`cmtg` for selector market templates and market template groups). `CommitClient` copies those rows with raw SQL, and the only message it sent was `OnClientChange` (Platform's single-client entry), so Sportsbook.API kept the copy it had loaded before the client existed. Whether a new client worked depended on when that cache last reloaded. Deleting the sportsbook-api pods fixed it, which confirmed the cause. `CommitClient` now sends, as its last step, the same messages the Sport Settings and Market Groups screens send after a save: `OnSportSortOrderChange` and `OnMarketSortOrderChange`, both with the new client's ID and code (CacheSync builds a routing key from the code). CacheSync forwards each one to every Sportsbook.API pod (DNS lookup, one call per pod IP), so no pod keeps a stale copy. The row records "Sportsbook caches cleared". If publishing fails, the client is still fully created and the row says to save its Sport Settings once instead. The three raw-SQL steps are now `protected virtual`, so `ClientImportRepositoryCacheRefreshTests.cs` can cover the commit path. Not changed: platform-web still crashes for any client whose sports list is empty, for example one with no Sport Settings at all; guarding `Index.razor.cs:458` would show an empty page instead.

**The import failed on DEV at "Trading DB registration" (October 27, 2026).** Every DEV create (PNX, KRK) failed at the last step with "Unexpected error: Invalid object name 'Client'". BO-42 retired `Trading.dbo.Client`: Backoffice and the odds engine now take clients from the Platform list, and flyway `V18__RetireTradingClientTable` (dev and stg, `V17` on prd) renames the table to `Client_DELETE_AFTER_BO42`. That migration's check ("no code of backoffice-final … references it") missed this raw `INSERT INTO [Client]` (`RegisterTradingClient`, using the Trading connection string), the last writer of the table. The step is removed; nothing reads that table any more. Without it, a failed row also never reached the Sportsbook cache clear, so that change could not be seen on DEV. **Deploy order:** deploy this before the retire migration runs on STG/PRD. On October 27 STG's Flyway history stopped at V15 and `Trading.dbo.Client` still existed there. Clients that failed only at this step (DEV PNX, KRK) are otherwise complete; their import rows stay Failed, and saving each one's Sport Settings once clears its sport-order cache.

**Deploy order:** the `OperationType` column (infrastructure flyway migration) must be applied in an environment **before** this backoffice build is deployed there — the existing Create import also writes `OperationType`, so it breaks without the column too.

**Deliberate scope limits (not fixed, tracked here):**
- `IsImportRunning` still locks per-environment regardless of operation type, so a Create batch and an Update batch can't run concurrently in the same environment even though they generally touch disjoint clients. Left as-is: it's the existing safety margin against two concurrent bulk writes hitting Platform/the DB at once, and splitting the lock needs its own concurrency design, not a quick patch alongside a data-corruption fix.
- `UpdateClientsModal.razor.cs` is largely parallel structure to `ImportClientsModal.razor.cs` (upload → validate → review → confirm). Not deduplicated in this pass — flagged for a follow-up refactor, not a correctness issue.
- `GetPlatformClients()` is hard-coded to region `"HK"`; a client that only exists on the `"SA"` Platform now fails its row (safe) rather than being updated. Whether any production clients are SA-only is unknown.
- The plain Edit Client modal still collapses "LimitId unknown" (Platform fetch failed on the Clients page) and "LimitId empty" into the same empty string, so a failed fetch there can still blank LimitId/Endpoint on save. Fixing it needs `AddOrUpdateClient` to distinguish null from empty.
- Update history filters `ActivityLog2` by `Page`/`Activity`/`Payload` prefix; that table's indexes cover only `TimeStamp` and `UserId`, so each "View changes" click likely scans it. An index on a shared production table deserves its own ticket.
- Lost updates: Platform, client and bet-setting data are read once at batch start, and each row then does a full overwrite — a manual edit made to the same client during a long batch is overwritten.
- Commit endpoints are gated by `[Authorize]` with no claim requirement; the `sbc-clients-canedit` check is UI-only (BO-130). `Environment` used to be client-supplied; since BO-133 the server's own environment is used (see below).
- History timestamps use `ToLocalTime()` on the server, so they show server time, not the viewer's.

**An import interrupted by a restart locked the environment (October 29, 2026, BO-133).** `CommitBatch`/`CommitUpdateBatch` save the batch as Running and close it only in their `finally`, which never runs when the process is killed (deploy, crash, node move). The batch stayed Running and every later import/update in that environment was refused until someone edited `ClientImportBatch` by hand. Fixes in `ClientImportRepository`:
- New status `ClientImportBatchStatus.Interrupted`. The batch's Succeeded/Failed counts are recounted from its rows; rows still `Pending` are the clients that were not done, and the Update history shows "N not done".
- On startup, `ClientImportRecoveryService` (a `BackgroundService`, so it never delays readiness) marks every Running batch of this server's environment as Interrupted. This relies on bo-api running **one replica** (chart default, no autoscaling). If bo-api ever scales out, this would close another pod's live batch: replace it with a per-batch heartbeat column. Also note that a developer running bo-api locally against the dev database does the same for Dev.
- Backstop: a Running batch older than `ClientImport:StaleRunningMinutes` (default 120) no longer blocks `IsImportRunning` and is marked Interrupted on the next commit. A batch still really running past the limit loses its lock, and its `finally` later overwrites the status with the real result.
- The environment comes from `GetEnvironment()` (the Backoffice database host). A request naming another environment is refused, and the batch and the audit log `From` use the server's value.
- Check-and-take is atomic through the filtered unique index `UX_ClientImportBatch_RunningPerEnvironment` (flyway V5 on dev/prd; stg's table predates flyway, so an idempotent infrastructure migration creates the index there if it is missing). The losing insert is caught (SQL 2601/2627) and returns the usual "already running" message instead of a 500.
- **Deploy order:** the new `Interrupted` value lives in Backoffice.Shared; deploy bo-api before bo-web (an older bo-web shows it as plain grey text, which is harmless). Apply the stg index migration before relying on the concurrency guarantee there.
- Tests: `Backoffice.API.Tests/ClientImport/ClientImportLockTests.cs`.

---

## Authorization Coupling — Shared `activitylog/list` Endpoint

**Date noted:** June 25, 2026

**Issue:** The generic activity-log read endpoint `POST api/config/activitylog/list` lives in `ConfigController`, which is gated at the class level by `[Authorize("SBC.OE.Config", View)]` (`Backoffice.API/Controllers/Backoffice/ConfigController.cs:24`). Three unrelated features call this single endpoint:

| Caller | Permission the feature uses | Correctly gated? |
|--------|-----------------------------|------------------|
| OddsEngine Config template logs | `SBC.OE.Config` | ✅ yes |
| OddsEngine Config Assigned / Unassigned league logs (BO-155) | `SBC.OE.Config` | yes |
| Market Settlement history | `Admin.Settlements.MarketSettlement` | ❌ was 401 |
| OddsEngine SportSettings "View Logs" | `SBC.OE.SportsSettings` | ❌ still 401 |

Any user holding the feature's own permission but **not** `SBC.OE.Config` receives a 401, which the Web layer silently turns into an empty result (e.g. Market Settlement history showed "No Record Found" even though the row existed in `ActivityLog2`). Verified end-to-end against staging with the `superadmin` account (no `SBC.OE.Config` claim).

**Resolved (Market Settlement):** Added a dedicated `POST api/trading/manual/marketsettlement/history` action on `TradingController`, gated on `[Authorize("Admin.Settlements.MarketSettlement", View)]`, reusing the same `IBackofficeRepository.GetActivityLogs` logic. The Web page now calls `IBackofficeService.GetMarketSettlementActivityLogs`. No change to the shared endpoint or its other callers.

**Remaining:** OddsEngine **SportSettings "View Logs"** (`Backoffice.Web/Components/OddsEngine/SportSettings/ViewLogsModal.razor.cs:67`) still calls the shared endpoint and is therefore blocked for users without `SBC.OE.Config`. `activitylog/manualbetsettlementlist` (also in `ConfigController`) carries the same latent coupling. Recommend the same dedicated-endpoint treatment.

---

## Shared `GetActivityLogs` Query Never Returned Rows on Staging (BO-113)

**Date noted:** October 2, 2026 (fixed same date, discovered while verifying BO-108)

**Issue:** `BackOfficeRepository.GetActivityLogs(ActivityLogRequest)` (`Backoffice.API/Repositories/BackOfficeRepository.cs:311`) - the same shared method behind the `activitylog/list` endpoint described above, and therefore every "View Logs" modal in Backoffice.Web (Client Site Config, OddsEngine templates/market groups/boosted settings/sport settings, user groups, Message Settlement, Cancel Bet History) - called the `GetActivityLogs` stored procedure with each parameter wrapped in quotes: `FromSql($"GetActivityLogs '{request.Page}', '{request.Payload}', '{fromUTC}', '{toUTC}'")`. Per the `FromSql` interpolation rule (`CLAUDE.md` critical constraints), this sends the literal text `'@p0'` as the parameter value instead of the real one. Confirmed via direct `EXEC` against Staging: the stored proc's own `CAST(@From AS datetime2)` then throws `Msg 241: Conversion failed when converting date and/or time from character string` - every call has always failed, not just mismatched. The repository's catch-all turns this into `StatusCode.Error`, and the Blazor pages treat that identically to a genuine empty result, so the symptom was indistinguishable from "no history exists" (a third masking of the same "No Record Found" pattern noted above, this time from a thrown exception rather than a 401).

**Resolved:** removed the quotes (`FromSql($"GetActivityLogs {request.Page}, {request.Payload}, {fromUTC}, {toUTC}")`). Verified directly against Staging with `EXEC GetActivityLogs 'Admin.ClientSiteConfig', '%', '2026-09-24 00:00:00', '2026-10-02 23:59:59'`: returns real rows, including entries with genuine `FullCashout` changes in the From/To JSON.

**Also resolved, same ticket:** the three other occurrences of the identical quoting bug - `BackOfficeRepository.cs:179` (`GetPriceboostActivityLogs`), `:246` (`GetManualBetSettlementHistoryLogs`), and `Backoffice.Utility/Repository/BackofficeRepository.cs:627`/`:695` (same two methods, Backoffice.Utility service). The two `ManualBetSettlement` occurrences pass Payload as a literal `''` written directly in the SQL text rather than an interpolated `{expression}`, so that one stays quoted deliberately (it is valid SQL empty-string syntax, not a parameterized value) - only the three genuinely interpolated parameters in each of those two methods had their quotes removed. Verified directly against Staging: the previously-throwing call pattern (`EXEC GetActivityLogs '@p0', '@p1', '@p2', '@p3'`) still throws `Msg 241` as expected (proving the bug is real and unrelated to which method calls it), and the corrected pattern with real values executes cleanly with no error.

**Residual:** the `GetActivityLogs` stored procedure is confirmed missing entirely in DEV (`sys.procedures` returns zero rows there), not only PRD as previously documented (GM-134) - calling it in DEV throws "could not find stored procedure" instead, caught by the same catch-all, so DEV's "View Logs" modals will still show empty until the proc is deployed there, independent of this fix.

---

## Sportsbook Static Translation — Add/Edit/Delete Were Non-Functional

**Date noted:** July 2, 2026

**Issue:** The "Sportsbook Static Translation" admin screen (`/translations/static`) only had a working Read. Add/Edit/Delete (`Backoffice.Web/Pages/SBC/StaticTranslation.razor.cs`) only mutated an in-memory `Dictionary` in the Blazor page — no HTTP call was made, so any change was lost on refresh, language switch, or when another admin opened the page. The delete icon was commented out in the markup, masking half of the gap from casual observation. The one real network call (Read) bypassed `Backoffice.API` entirely, calling an external, customer-facing "Sportsbook Platform API" (`platform-final` repo) directly — and that API itself had no write endpoints for this data (`SportsbookStatic` table) either. Confirmed via live Staging Swagger inspection (no route existed) and `POST`/`PUT`/`DELETE` against the only related routes returning `405`.

**Resolved:** `Backoffice.API` now owns full CRUD directly against `SportsbookStatic` in the shared `Translation` SQL database (confirmed to be the same physical database platform-final's `Platform.Sportsbook.API` uses, via identical connection strings and ArgoCD secret paths). Added `DbSet<SportsbookStatic>` to `Backoffice.Shared/Data/Translation/TranslationContext.cs`, three repository methods on `IBackofficeRepository` (`GetSportsbookTranslations`, `AddOrUpdateSportsbookTranslation`, `DeleteSportsbookTranslation`) mirroring the existing `AddOrUpdateConfigTemplate`/`DeleteConfigTemplate` patterns, and three endpoints under `ConfigController` (`GET/POST api/config/sportsbook/translations`, `POST api/config/sportsbook/translations/delete`), originally gated on the existing `SBC.OE.Config` claim (now `Translations.Static` — see "Dedicated Translation Permission" below). `Backoffice.Web` now calls these endpoints for Get/Add/Edit/Delete instead of the external platform API and the in-memory mock; the delete icon is re-enabled. Verified end-to-end (insert → read-back → update → read-back → delete → read-back → delete-of-missing-key) directly against the Staging `Translation` database.

**Deliberate scope limits:**
- `platform-final` / `Platform.Sportsbook.API` were intentionally left untouched (per explicit instruction) — the retail sportsbook frontend's own translation read path is unaffected.
- No caching was added for the new endpoints (dataset is ~322 rows; a feature whose purpose is editing this data shouldn't fight a stale read cache), unlike the `IMemCache` pattern used by `GetTranslationsByLanguageCode` for the unrelated `BackofficeStatic` table.
- Consequence of the above: platform-final's own 24h in-process cache for `SportsbookStatic` means an edit made via this new Backoffice CRUD will not appear on the live retail sportsbook site until that cache naturally expires (or someone manually triggers platform-final's existing `v1/sportsbook/translation/recovery` endpoint). Only the Backoffice admin screen reflects changes immediately.

---

## Sportsbook Dynamic Translation — CRUD Against `TranslationCache2`

**Date noted:** July 2, 2026. **Revised:** July 4, 2026 — schema changed underneath this feature after the initial implementation; see below.

**What changed (July 2):** The "Dynamic Translation" admin screen (`/translations/dynamic`), previously 100% hardcoded mock markup with no backend at all, got full CRUD. The initial implementation joined `SportsbookGlossary` (English source terms, `bet-genius`-owned) to `TranslationCache2` (then a narrow `(StringId, TargetLanguage, TargetText)` bridge table) — see git history for that version if needed.

**What changed (July 4) — schema pivot:** Live inspection of the Staging `Translation` database (`INFORMATION_SCHEMA.COLUMNS` against `dbo.TranslationCache2`) showed the table had been reshaped independently of `bet-genius`'s own code (no corresponding commit in that repo — this is schema drift, not a released change): it's now self-contained, carrying its own `SOURCE_LANGUAGE`/`SOURCE_TEXT` plus **fixed per-language columns** `TARGET_TEXT_ZH`, `TARGET_TEXT_ES`, `TARGET_TEXT_FR`, and its own `HAS_BET_PLACEMENT` flag — no join to `SportsbookGlossary` needed or possible via the old key (`STRING_ID` is now the sole unique key, not `(StringId, TargetLanguage)`).

Backoffice.API was updated to match:
- `SportsbookGlossary` was removed entirely from `backoffice-final` (entity, `DbSet`, Fluent config, and all repository/controller code) — nothing in this repo reads or writes it anymore.
- `Backoffice.Shared/Data/Translation/Entities/TranslationCache2.cs` rewritten to the new columns; `TranslationContext.cs`'s unique index is now on `StringId` alone.
- `AddOrUpdateSportsbookGlossary` and the old separate `AddOrUpdateGlossaryTranslation` endpoint were collapsed into one upsert (`POST api/config/sportsbook/glossary`) that takes `SourceText` plus one `TargetLanguage`/`TargetText` pair and writes to the matching `TARGET_TEXT_ZH/ES/FR` column via a small switch helper (`BackOfficeRepository.GetTargetText`/`SetTargetText`) — editing one language never touches the other two columns on that row (verified directly against Staging).
- The `HasBetPlacement` delete guard now reads from `TranslationCache2.HasBetPlacement` directly (it's on this table now, not via a join to glossary).
- Language dropdown corrected to the real supported set (EN/ZH/ES/FR) — it previously listed EN/JA/BR, copy-pasted from Static Translation and never actually matched any real target language for this feature.

**Ownership is now unclear — flag for follow-up:** because this is schema drift with no `bet-genius` commit behind it, it's not established whether `bet-genius`'s pipeline is even aware of or intends to write this new shape. As of this change, `TranslationCache2` had 0 rows in Staging and Backoffice is the only known writer. If `bet-genius` later starts writing this table too (or the schema changes again), re-verify before assuming this repo's mapping still matches — there's no in-repo migration tracking this table (per `bet-genius`'s own script comments, this DB's schema is "managed externally... apply via change control").

**Related but untouched:** `SportsbookGlossary` (still `bet-genius`-owned, ~10,600 rows and growing) and `SportsbookDynamic` (a much older, much larger legacy table, ~10.7M rows in Staging) are unaffected by this change — neither is referenced anywhere in `backoffice-final`.

---

## Sportsbook Dynamic Translation — Bulk Export/Import (CSV)

**Date noted:** July 9, 2026

**What changed:** The "Dynamic Translation" screen had no bulk edit path — every glossary term had to be added/edited one row at a time via `AddOrUpdateSportsbookGlossary`. Added `ExportSportsbookGlossary` (returns every `TranslationCache2` row, unpaginated, as JSON) and `ImportSportsbookGlossary` (upserts rows parsed from an uploaded file) to `IBackofficeRepository`/`BackOfficeRepository`, with two new `TranslationController` actions (`GET api/translation/sportsbook/glossary/export`, `POST api/translation/sportsbook/glossary/import`).

Deliberately mirrors, rather than upgrades, Static Translation's existing "Export to Excel" mechanism (`StaticTranslation.razor.cs:281-311`) — that button produces plain CSV (not a real `.xlsx`) built client-side with a `StringBuilder` + UTF-8 BOM + `downloadFileFromText`. Dynamic Translation's export follows the same mechanism, just widened to one column per language (`StringId,EN,ZH,ES,FR`) since the file also has to work as an upload template; both the CSV building and parsing live in `Backoffice.Web/Services/GlossaryCsvHelper.cs`, not the API — the API only ever receives/returns structured JSON rows, never raw file content. No new NuGet dependency (no Excel library) was introduced.

**Idempotent upsert semantics (the point of the feature):** for each row, a target-language (ZH/ES/FR) cell is written only if it is non-blank, differs from that row's `SourceText`, and differs from the value already stored — otherwise it's skipped. Concretely: a blank cell leaves the existing translation untouched (partial re-uploads can't accidentally wipe other languages), a cell equal to the English source text is treated as "not yet translated" and skipped, and re-uploading an unmodified export is a total no-op (no DB writes, no activity-log rows, no `OnTranslationChange` publish). New `StringId`s are created (true upsert) provided the EN column is populated; `SourceText` on an *existing* row is read-only reference data for that comparison, never overwritten by the file. Duplicate `StringId`s within one uploaded file: first occurrence wins, the rest are reported back as skipped rows.

**Validation:** `Backoffice.API.Tests/Translation/TranslationRepositoryTests.cs` covers create/update/skip/idempotent-reupload/blank-cell/duplicate-row cases against the repository (in-memory EF Core DB); `Backoffice.Web.Tests/Services/GlossaryCsvHelperTests.cs` covers the hand-rolled CSV parser against RFC4180 edge cases (embedded commas, quotes, newlines, BOM, mismatched header). All new and pre-existing tests pass (`dotnet test` on both projects); `Backoffice.API`, `Backoffice.Web`, and both test projects build with 0 errors. Not yet exercised against a live Staging database or through the browser — see Manual Verification note in the implementation summary.

**Update (July 15, 2026, BO-82):** the export now honors the page's active filters instead of always returning the full list. `GET api/translation/sportsbook/glossary/export` gained two optional, backward-compatible query params (`targetLanguage`, `untranslatedOnly`) alongside the existing `searchText`; when `untranslatedOnly=true` and the language is ZH/ES/FR, rows are filtered with the exact same "untranslated" definition as the grid (target text blank or equal to source, letterless source texts excluded), via a shared `ApplyUntranslatedFilter` helper in `BackOfficeRepository`. The CSV shape is unchanged (all language columns are always present so the file still works as the import template); only the row set narrows. `DynamicTranslation.razor.cs` passes the selected language and the "show only untranslated" toggle to the export call, mirroring the grid request.

**Update (July 16, 2026, BO-82) - exact match search:** the search box gained an "Exact match" checkbox. Unchecked (default) keeps the original contains-search; checked, the grid and export filter with `StringId == SearchText OR SourceText == SearchText`, so searching `1` returns only the rows whose String ID or Source Text is exactly `1`. Wiring: new optional `ExactMatch` bool on `GetSportsbookGlossaryRequest`, an equality/contains branch in `BackOfficeRepository.GetSportsbookGlossary` and `ExportSportsbookGlossary`, and an optional `exactMatch` query param on the export endpoint - all additive and backward compatible. Both comparison modes follow the database collation for case sensitivity, so they behave consistently with each other.

**Update (July 16, 2026, BO-82) - Static Translation restructured the same way:** the Static Translation screen (`/translations/static`) now mirrors the Dynamic grid: the dropdown offers only ZH/JA/BR (default ZH), and the grid shows Key | Source Text (EN) | Translation. The page loads two dictionaries via the existing `GET api/translation/sportsbook/translations` endpoint (EN + selected language) and joins them by key client-side; the endpoint already returns every known key with blank placeholders, so no read API changed. "Untranslated" stays defined as *blank target value* (unlike the glossary, a static value legitimately equal to English is common). Search covers Key/Source/Translation with the same "Exact match" checkbox; export CSV becomes `Key,EN,<language>` honoring the active filters; the EN stats badge is hidden. The edit modal now edits both the EN source and the translation (one `AddOrUpdateSportsbookTranslation` call per language that actually changed). Delete semantics changed deliberately: the trash icon now removes the key across ALL languages via a new `POST api/translation/sportsbook/translations/delete-key` endpoint (`BackOfficeRepository.DeleteSportsbookTranslationKey` - one activity log entry, one `OnStaticTranslationChange` publish per affected language). Previously delete removed only the selected language's row, so fully retiring a key took one pass per language - and with EN hidden the EN row would have been unreachable. The per-language delete endpoint remains for backward compatibility; clearing a single translation is done by editing it to blank.

**Update (July 16, 2026, BO-82) - EN removed from the language dropdown (Web only):** users found the permanently disabled "Show only untranslated" checkbox in the EN view confusing, and the EN view added nothing: the grid always shows a "Source Text (EN)" column, so with EN selected the "Translation (EN)" column merely duplicated it, the edit modal hid its translation field, and the stats badges never included EN. `DynamicTranslation.razor(.cs)` now offers only ZH/ES/FR (default ZH), the untranslated checkbox is never disabled, and the dead `SelectedLanguageCode != "EN"` guards were removed from the page. Deliberately not applied to Static Translation: there EN is a real editable per-language dataset (Key/Value, no separate English column), so its EN entry must stay. Server-side EN handling (`GetSportsbookGlossary`, `AddOrUpdateSportsbookGlossary`, export) is untouched for backward compatibility.

---

## Sportsbook Dynamic Translation — Letterless Terms Are Never "Untranslated"

**Date noted:** July 14, 2026

**Issue:** "untranslated" was defined as *target cell blank or equal to the EN source*. Glossary terms whose `SourceText` contains no letters (`5+`, `4`, `0-1`, …) read the same in every language, so they could never leave that state — they permanently polluted the "Show only untranslated" filter, the yellow "— missing —" highlight, and the per-language `translated/total` stat badges (which could therefore never reach 100%).

**Resolved:** a term now counts as untranslated only if its `SourceText` contains at least one letter (a–z/A–Z). Applied consistently in three places: the `UntranslatedOnly` filter and the stats counts in `BackOfficeRepository` (`GetSportsbookGlossary`/`GetSportsbookGlossaryStats`), and the row highlight in `DynamicTranslation.razor` (a letterless row shows its source text instead of "— missing —"). The UI-side rule lives in `Backoffice.Shared/Utilities/TranslationTextHelper.RequiresTranslation`; the query side is a hand-built expression (`BackOfficeRepository.SourceTextRequiresTranslation`) because `char.IsLetter`/regex don't translate on EF Core 3.1 and `LIKE '%[A-Za-z]%'` ranges are SQL Server-only (the InMemory provider used by the tests treats them as literals) — it compiles to `SourceText IS NOT NULL AND (LOWER(SourceText) LIKE CHARINDEX 'a' OR … 'z')`. Export/import semantics and the external updater script are untouched: a letterless row is only excluded from the *untranslated* views, never from the grid or the CSV.

**Validation:** new repository tests (filter excludes letterless rows, unfiltered view still returns them, stats count them as translated) and a bUnit test (letterless row untinted, shows its value) — 46 API + 33 Web tests pass, solution builds with 0 errors.

---

## Dedicated Translation Permission (replaces borrowed `SBC.OE.Config`)

**Date noted:** July 4, 2026

**Issue:** Both Translation screens were gated behind `SBC.OE.Config` (OddsEngine Config Templates' permission) as a stopgap, to avoid inventing a new, unseeded claim. Staging testing then hit a `401 Unauthorized` on Dynamic Translation with the `Super Admin` account — and cross-checking confirmed the **pre-existing** OddsEngine Config Templates endpoint (`GET api/config/templates`) 401s with the exact same token too. This is the same latent gap already documented above ("Authorization Coupling" section, line 24). Borrowing `SBC.OE.Config` for Translation didn't save a permissions-setup step, and coupled two unrelated feature areas' access control.

**Resolved (permission definition):** Added a dedicated `TranslationsPermission` enum (`Backoffice.Shared/Enums/PermissionCategory.cs`) under the previously-reserved-but-unwired `PermissionCategory.Translations` (value `4`) — `Translations_Static` and `Translations_Dynamic`, converted to claim types `Translations.Static`/`Translations.Dynamic`. `scripts/sql/seed-translations-permission.sql` seeds the corresponding `Permission` rows in the Auth database (no migration/seed mechanism exists there — confirmed, rows are hand-inserted reference data; no API exists to create a permission definition, only to grant an existing one to a role).

**How `Super Admin` actually gets new permissions — corrected understanding:** `Super Admin` is **hidden from the "Manage User Groups" screen entirely** (`Auth.Shared/Services/AuthService.cs:180-181` hardcodes `ShowSuperAdmin=false` at every Backoffice.Web call site — there is no UI way to reveal it). It does **not** need an explicit grant there anyway: `Auth.Api/Repositories/AuthRepository.cs:107-110` auto-populates a `Super Admin` JWT with **every** `Permission` row where `CategoryId != 99` at login time. `CategoryId = 99` is a real, deliberate "excluded even from Super Admin" bucket — confirmed via direct query that `SBC.OE.Config` itself is `CategoryId = 99` (3 rows total), which is *why* Super Admin never had it, by design, not by oversight. `Translations.Static`/`Translations.Dynamic` are `CategoryId = 4`, so once their `Permission` rows exist, `Super Admin` picks them up automatically on next login — confirmed via a fresh JWT decode showing both claims present with no explicit `RoleClaims` grant needed.

**Second bug found during Staging verification — class-level/method-level `[Authorize]` accumulation:** even with the correct claims present in a fresh JWT, requests still 401'd. Root cause: `Backoffice.API/ActionFilter/AuthorizeAttribute.cs` is a hand-rolled `IAuthorizationFilter`, not ASP.NET Core's policy-merging `[Authorize]`. `ConfigController.cs:24` carries a class-level `[Authorize("SBC.OE.Config", View)]`; when an action on that controller also carries its own method-level `[Authorize("Translations.*", ...)]`, **both filter instances run independently** — the controller-scoped one executes first, and since it always rejects (Super Admin never has `SBC.OE.Config`, per the `CategoryId=99` exclusion above), the pipeline short-circuits before the method-level filter — the one that would have correctly approved the request — ever runs. `ConfigController.cs` was the only controller in the codebase where a class-level claim differed from a method-level one, so this exact failure mode had never been exercised before.

**Resolved (routing):** moved all 6 Static/Dynamic Translation endpoints to a new `Backoffice.API/Controllers/Backoffice/TranslationController.cs` with **no class-level `[Authorize]` default** — each action keeps its own `Translations.Static`/`Translations.Dynamic` check (View for reads, Edit for writes) with nothing else to conflict with. This changed the route prefix from `api/config/sportsbook/...` to `api/translation/sportsbook/...` — `Backoffice.Web/Services/BackOfficeService.cs`'s 6 hardcoded URLs were updated to match; both API and Web must deploy together. Verified end-to-end locally (Auth.Api + Backoffice.API both run locally, logged in against the local Auth.Api directly to get a self-consistent token) — both endpoints now return `200 Success` with real data using a token that only carries the new `Translations.*` claims.

**Sequencing for this whole change:** (1) run `scripts/sql/seed-translations-permission.sql` against the Auth database, (2) any **non-Super-Admin** role that needs this feature must be granted `Translations.Static`/`Translations.Dynamic` explicitly via Manage User Groups (Super Admin does not need this step — see above), (3) deploy this `backoffice-final` code (API + Web together, since the route changed), (4) fresh login to pick up the new JWT claims.

**If this general class-level/method-level accumulation issue recurs elsewhere:** the safest fix per-incident is what was done here (move the conflicting action to a controller with a compatible or absent class-level default) rather than modifying `AuthorizeAttribute.cs` itself, which is shared by every controller in the API and wasn't touched here to keep this change's blast radius contained to the Translation feature.

**Follow-up (July 5, 2026) — Web/UI wiring:** the permission existed but was invisible and unenforced in `Backoffice.Web`:
- **Manage User Groups had no Translations section**, even after seeding. Root cause: `Auth.Api/Repositories/AuthRepository.cs:761`'s `GetPermissionList` defaults to `categories = "0,1,2"` (Admin/SBC/PMS) when the caller omits the param, silently dropping category `4`. Fixed in `backoffice-final` only: `Backoffice.API/Repositories/AuthenticationRepository.cs` now passes `categories=0,1,2,4` explicitly (Auth's endpoint already accepted the param — no Auth deployment needed). The permission-tree UI (`Backoffice.Web/Components/Common/Permission.razor(.cs)`) is fully data-driven by category, so the Translations section renders with no structural UI change; only a category description label was added. Category `3` (OE) remains intentionally excluded, matching the previous behavior.
- **Blazor-side enforcement added** (`Backoffice.Web/Extensions/WebApplicationBuilderExtensions.cs`): `translations-static`, `translations-static-canedit`, `translations-dynamic`, `translations-dynamic-canedit` policies, mirroring the existing `sbc-*`/`admin-*` pattern. `/translations/static` and `/translations/dynamic` previously used a bare `<AuthorizeView>` (any logged-in user could open them and see Add/Edit/Delete); they now require the View-or-Edit policy for the page and the `-canedit` policy for Add/Edit/Delete controls.
- **Nav menu was unconditional** (`Backoffice.Web/Shared/LayoutBase.cs`): the Translation menu + both submenus were appended for every user regardless of claims. Now each submenu is added only if the corresponding `Translations.*` claim is present and non-`"0"`, and the parent menu only if at least one submenu survives. Consequence: after this deploys, the Translation menu disappears for any user whose JWT lacks the claims — i.e. everyone until the seed script has been run and (for non-Super-Admin roles) the permission granted + fresh login, per the sequencing above. This is the intended fail-closed behavior.

---

## Maintenance Page — Dead `bl` Local-File Branch Broke the Whole Fan-Out (BO-2)

**Date noted:** August 3, 2026 (fixed same date under BO-2)

**Issue:** `GET/POST api/config/maintenance` failed on Staging (and, by identical config, Production) with `Could not find a part of the path '/app/Configs/maintenance.json'`, which the MaintenanceV2 page surfaced as an empty/broken client dropdown ("works on my local, not on staging", BO-2). Root cause: `MaintenanceRepository` fanned out over `Maintenance:Apps` = `el1, el2, el3, al, el4, bl`; the five layout apps go through the Dapr-style invoke (`/v1.0/invoke/{app}/method/maintenance`, each guarded by its own try/catch), but `bl` read/wrote a **local file** `Configs/maintenance.json` on the BO API pod with no existence check and no per-app isolation — the thrown exception hit the outer catch and discarded the results of all other apps. The file was never shipped (no `Configs/` folder in the repo, no csproj copy rule, no volume mount in argocd stg/prd), so the `bl` path could never work in a deployed container. Even if it had, the write went to the pod's ephemeral local disk that no other service reads — the frontends read their **own** local `Configs/maintenance.json` on their own pods — so the `bl` branch was dead functionality, not just broken.

**Resolved:** removed the `bl` special-case from `GetMaintenanceV2`/`SetMaintenanceV2` (all apps now go through the uniformly-guarded Dapr path), dropped `bl` from `Maintenance:Apps` and the dead `Custom:MaintenanceConfigFile` key in `appsettings.json`, and changed the first-time-setup seeding fallback to use the first configured app instead of hardcoded `"BL"`. Also fixed the related Startup.cs literal api-key bug (item 18 below). Covered by `Backoffice.API.Tests/Maintenance/MaintenanceRepositoryTests.cs` (per-app fault isolation, seeding fallback, upstream-failure passthrough, Set fan-out payload).

**Residual unknowns:** the source of `bo-maintenance-api` (the Dapr-style router the BO API calls, `API:Maintenance:Uri`) is not in any sibling repo, and whether it validates the `x-api-key` header is unverified. What the `el1–el4`/`al` apps return on Staging can only be confirmed after this fix is deployed.

**Follow-up (same ticket, August 2026) — migrated to webprops-api:** `bo-maintenance-api` was confirmed no longer deployed (absent from every argocd app list; its source exists in no sibling repo; the frontends never exposed the `/maintenance` endpoints it would have routed to — platform-web's `MaintenanceReponseDTO`/`SetMaintenanceRequestDTO` are dead code). `MaintenanceRepository` now targets webprops-api directly (`API:WebProps:Uri`, already configured in argocd stg/prd, so **no argocd change was needed**): `GET/POST /api/v1/maintenance` with `X-Layout: {app}` per configured app, bare-array response parsing, matching S3 clientIds by client code or platform Guid. Missing `{LAYOUT}/{CLIENTID}.json` objects are created on demand (stub via `POST /api/v1/config/{code}` with `X-Layout`) before setting a flag — idempotent by design. `GetMaintenanceV2` union-seeds clients without config objects (per app, `IsMaintenance=false`) so the page is fully operable from an empty bucket. Remaining gap: no frontend consumes webprops maintenance flags yet (see `docs/15_ReferenceRepositories.md`); `ALL` requests only affect clients whose config objects already exist.

---

## League Sorting (BO-107) - Oddstable Order Is Written but Not Yet Honored Downstream

**Date noted:** October 8, 2026

**Issue:** BO-107 adds a per-client, per-sport Oddstable league order (`Sportsbook.dbo.ClientOddstableLeagueOrder`, independent from the Sport Tree's `ClientLeagueOrder`). Backoffice writes it and publishes `OnOddstableLeagueSortOrderChange` (`r.oe.cachesync.onoddstableleaguesortorderchange`), but nothing downstream reads the table or consumes the message yet, so saving an order has no visible effect on the Oddstable until the sibling-repo work below lands. Found while scoping BO-107 across `platform-final`, `odds-engine-2` and `platform-web` (read against each repo's `origin/main`).

**Gaps in the sibling repos (each needs its own change and PR):**

| Where | Gap |
|-------|-----|
| `platform-final` `Platform.CacheSync.Service/Worker.cs` | The message-type `switch` has no case for `OnOddstableLeagueSortOrderChange` and no `default`, so the message is logged and dropped (harmless to publish early, but it does nothing) |
| `platform-final` `Platform.Shared/Enums/CacheType.cs` | Needs `OddstableLeagueSortOrder = 9`. `Platform.Shared.Test/Contracts/enum-contracts.approved.txt` snapshot-tests every enum, so the approved file must be updated in the same PR |
| `platform-final` `Platform.Sportsbook.API` | `GetClientFixtureSortOrder` (`v1/sportsbook/fixturesortorder`) currently just returns the Sport Tree's `ClientLeagueOrder`; it has to read the new table. The `SportSortOrder` purge also calls `DeleteCache("seo")`/`("leo")` against per-client keys (`seo-{clientId}`/`leo-{clientId}`), so only the `"so"` purge matches and segment/league order can stay stale for up to 24 hours |
| `platform-final` `CacheSyncRepository` | Only `ProcessMarketSortOrderChange` republishes onward to `r.oe.sb.engine.*`; a sport/league order change never reaches the engine |
| `odds-engine-2` `OE.Websocket.Service` | No handler for a league order change (only `OnMarketSortOrderChange` has one), and the per-league `fso` cache is refreshed only at startup and for newly discovered clients. The `SoOr` field is already streamed in every live odds-change message and `platform-web` already consumes it, so `platform-web` needs no change |
| `CacheType` enum | Hand-copied in three repos with no shared package. `odds-engine-2`'s copy already lacks `StaticTranslation = 8`; `OddstableLeagueSortOrder = 9` has to be added to each copy by hand |

**Audit log:** `SportsbookRepository` writes the audit entries itself, right after the save (one entry for a save that changed something, none for a save that changed nothing, one per target client for a copy), so a UI path cannot skip them and the previous order is read at write time. `ActivityLog2` lives in the Backoffice database and the order table in Sportsbook, so the two writes cannot share a transaction: if only the audit write fails the change stays saved, the failure is logged (`League Sorting audit log could not be written`) and the response carries a warning. The Logs dialog deliberately does not use the shared `GetActivityLogs` read (`BackOfficeRepository.cs:297`): it depends on a stored procedure that is not versioned in `infrastructure/flyway` and is recorded as missing in DEV (CLAUDE.md, BO-113), so it reads `ActivityLog2` with a plain query instead. BO-122 Import writes an `Import` entry the same way (one entry for a commit that changed the order, none when the order is already the same); Export reads what the page already shows and is not audited.

**Import and Export (BO-122) - known limits:**

- **Access level is not enforced by the endpoint.** `ClientController` carries a bare `[Authorize]` (`ClientController.cs:17`), which checks the token, not the `SBC.OE.LeagueSorting` access level. The page hides the Import icon from View users, but `POST sportsbook/client/oddstableleaguesettings/import` (like the BO-107 save and copy endpoints) accepts any valid token. Closing it means a claim-checking `[Authorize("SBC.OE.LeagueSorting", ...)]` on those actions.
- **Untestable seam.** The sport's segment and league lookups are static MongoDB collections initialised in the `SportsbookRepository` constructor, so the import reads them through `protected virtual GetOddstableLeagueCatalog`, which the tests override (`CatalogSportsbookRepository` in `ClientOddstableLeagueOrderRepositoryTests`). The catalog join itself (segments without leagues and leagues without a known segment are left out, as in the Edit dialog) is therefore not covered by a unit test; the merge rules are (`OddstableLeagueImportPlannerTests`).
- **Validate and commit are separate requests with no version check.** The commit recomputes everything from the same rows, so if the client's order or the lookup changes between the review and the click, the commit applies to the new state (last write wins, as for Edit).
- **50-league cap lives in two places.** `OddstableLeagueImportPlanner.MaxLeaguesPerClient` (API) and `LeagueSortingHelper.MaxLeaguesPerClient` (Web). The Edit endpoint does not enforce it server side; only the dialog and the import do.
- **Export does not neutralise spreadsheet formulas.** League names starting with `=`, `+`, `-` or `@` are written as they are (the Glossary export behaves the same). The names come from the feed lookup, not from users.

**Permission and table rows (infrastructure repo):** the page is gated on the claim `SBC.OE.LeagueSorting`, which only exists once a `Permission` row with that `ClaimType` is in the Auth database of the environment (`CategoryId = 1`, so Super Admin receives it automatically at its next login, see "Dedicated Translation Permission" below). Manage Users and Manage User Groups build their permission tree from those rows (`Backoffice.Web/Components/Common/Permission.razor.cs`), so no BO code is needed for it to appear under Odds Engine > Sports Settings. The row ships as flyway migrations in the infrastructure repo: `V14__AddLeagueSortingPermission` for dev and stg and `V13__AddLeagueSortingPermission` for prd (idempotent, and the DEV bootstrap seed `R__Seed_010_Auth_Permissions.sql` carries the same row), applied by hand with `just migrate <env>` from `flyway/`. The order table is `V13__CreateClientOddstableLeagueOrder` for dev and stg and `V12__CreateClientOddstableLeagueOrder` for prd (additive and idempotent, so it is safe to apply ahead of the release). Each environment applies the table first and the permission second. Until the permission row is applied nobody sees the page or the new Sports Settings dropdown, and until the table exists saving fails.

**Deployment order when the downstream work is done:** `platform-final` (enum + CacheSync case + read path) before enabling the feature for clients; Backoffice can ship first because publishing the new message type early is safely ignored.

**Status:** open. Backoffice side tracked under BO-107; the sibling-repo changes need their own tickets.

---

## Back Office Web Rendered Stored and Hub Text as HTML (BO-134)

**Date noted:** October 30, 2026

**Issue:** `Backoffice.Web` had 36 places that render a string as raw HTML (`MarkupString` x35, `Html.Raw` x1) and no HTML encoding anywhere. Several were fed by text other users type or by the hub, so a user group name such as `<img src=x onerror=...>` ran script in the browser of the next admin who opened the delete confirmation or the group's change log, with that admin's permissions. The live notification pop-up rendered `NotificationsModel.Details` raw, so anyone able to publish to the hub (open before BO-131) could do the same to every connected admin. The login page also wrote `<script>console.log(...)</script>` on every failed login with the serialized `LogInModel`, password included.

**Resolved (BO-134):**
- `Helpers/MarkupHelper.Encode` is applied to every stored or external value interpolated into a markup string: user group delete confirmation and logs (`CommonService.GetProperties` and the claim lines), market group logs, the price boost, boosted, sport settings, config and template log viewers, the copy client settings and close match confirmations, the manage user success alert, the manual, resend and add-reference settlement event details, and the login user agent. `MarkupString` now only wraps literal tags plus encoded values.
- The toast renders `MarkupHelper.ToSafeMarkup(message)`: the message is encoded except `<b>`, `<strong>`, `<br>` and `<a href="http(s)://...">text</a>`, which are rebuilt without any other attribute. That keeps the two formats producers send (Utility's excel download link, the API's bold boosted settings notice) and the message settlement result. The Utility link loses its inline `onmouseover` colour change.
- `Tooltip` renders `Text` as text (no caller passes markup). `CustomModal`'s `ModalIcon` stays raw: only developer literals are passed.
- The login console script is gone; failed logins are logged server side (`Log.Warning`, username, status and message, no password).
- Group names containing `<` or `>` are refused in Manage User Groups (Web only).
- Every response carries `Content-Security-Policy: object-src 'none'; base-uri 'self'; frame-ancestors 'self'` and `X-Content-Type-Options: nosniff` (`WebApplicationExtensions.UseSecurityHeaders`).
- Tests: `Backoffice.Web.Tests/Common/MarkupEncodingTests.cs` (delete modal, group log, toast, allow-list).

**Still open:**
- No `script-src` policy, so CSP does not yet block inline handlers. `_Host.cshtml` and `_Layout.cshtml` have inline `<script>` blocks that must move to files (or get nonces) first, and the policy needs checking against Metabase, Tableau and the oddin script in a browser.
- The `<` `>` rule is Web only; the Auth API that stores group names was not checked. Client and template names have no such rule.
- Producers still send markup in `NotificationsModel.Details`. A structured link field would let the toast drop the allow-list.
- Translations (`TranslationState.GetValue`) are treated as trusted literals; they come from the static translation tables that admins edit.

---

## CSV Exports Wrote User Text That Excel Runs as a Formula (BO-137)

**Date noted:** November 2, 2026

**Issue:** The CSV exports (clients, client update error report, static translations, dynamic translations, league sorting) wrote user text unchanged. A cell that starts with `=`, `+`, `-` or `@` is run as a formula when the file is opened in Excel, so a user who can edit a translation or a client name could make the admin who exports the file send its other cells to an outside address or show a misleading link. Three copies of `EscapeCsv` only doubled quotes and quoted the field.

**Resolved (BO-137):**
- `Helpers/CsvField.Escape` is the one escape for every export (`ClientCsvHelper`, `GlossaryCsvHelper`, `StaticTranslationCsvHelper`, `LeagueSortingCsvHelper`). A cell that starts with `=`, `+`, `-`, `@`, tab or carriage return gets a leading `'`. Text that already starts with `'` before one of those characters gets one more `'`, so the marker can always be told apart.
- `CsvField.RemoveFormulaMarker` removes exactly that one `'` on import: in `ParseCsvLines` of the glossary and static translation helpers (league sorting uses the glossary parser) and in `Get` of `ClientImportCsvParser` and `ClientUpdateCsvParser`. An unedited export imports back unchanged; `-5`, `+63 ...` and `'5` typed in a file are left alone.
- Number columns (client fees and limits, ids, sort order) are written as numbers and not marked.
- Tests: `Backoffice.Web.Tests/Common/CsvFieldTests.cs`.

**Still open:**
- Not checked in Excel. Excel may show the `'` as part of the text when it opens a CSV, rather than hiding it as it does for typed input. The data is still safe and round-trips; only the display differs.
- The Excel downloads in `Backoffice.Utility` (EPPlus, `.Value =` with strings) store text, not formulas (assumed, not tested); they are not produced at the moment.

---

## Critical Issues (Fix Immediately)

### 1. Developer Exception Page Always Enabled (resolved, BO-132)

**Location:** `Backoffice.Web/Program.cs` (was line 61, under a `// temp` comment)
```csharp
// temp
app.UseDeveloperExceptionPage();  // ALWAYS ENABLED
```
`AddServerSideBlazor().AddCircuitOptions(o => o.DetailedErrors = true)` also sent circuit exception text to the browser in every environment.

**Severity:** 🔴 CRITICAL (until BO-132)

**Impact (before the fix):** stack traces, internal paths, request headers and cookies were shown to any user who hit an error, in every environment including PRD.

**Fix (BO-132):** `WebApplicationExtensions.UseErrorPages` (`Backoffice.Web/Extensions/WebApplicationExtensions.cs`)
runs the developer exception page only when the environment is Development or the `DetailedErrors` key is on;
otherwise it runs `UseExceptionHandler("/Error")` and `UseHsts()`, and the exception is logged by the handler
middleware (Loki). The circuit override was removed, so `CircuitOptions.DetailedErrors` follows the same key
(the framework binds it). No argocd values file sets `ASPNETCORE_ENVIRONMENT`, so DEV, STG and PRD all run as
Production: DEV keeps the detailed view through a `DetailedErrors: "true"` env entry in
`argocd/apps/corp-apps/bo-web/dev.yaml`; STG and PRD must not set it. Covered by
`Backoffice.Web.Tests/Pipeline/ErrorPagesTests.cs`.

**Still open:** `Backoffice.API/Startup.cs` runs `UseSwagger()` / `UseSwaggerUI()` in every environment.

---

### 2. Database Credentials in Configuration File

**Location:** `Backoffice.API/appsettings.json` line 60+
```json
"ConnectionStrings": {
    "Backoffice": "Data Source=stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com, 1433;
        Database=Backoffice;User Id=admin;Password=<redacted>;..."
}
```

**Severity:** 🔴 CRITICAL

**Impact:**
- Plaintext database passwords in source code
- Visible to anyone with repo access
- Reused across environments
- Already in version control (hard to remove)

**Risk:**
- Database compromise
- Data breach
- Compliance violations (SOC2, GDPR, PCI-DSS)

**Remediation Required:**
- Rotate all database passwords immediately
- Move to secrets manager (Azure Key Vault, AWS Secrets Manager)
- Update CI/CD to use environment variables
- Audit access logs

**Status (DEVOPS-371, 2026-10):** removed from the working tree, **not rotated**.
- Every committed `appsettings.json` now holds placeholders instead of real values:
  `Password=${DB_PASSWORD}` in connection strings, `${API_KEY}`, `${JWT_KEY}`,
  `${RABBITMQ_PASSWORD}`, `${AZURE_STORAGE_CONNECTION_STRING}`, `${METABASE_SSO_SECRET_KEY}`, `${METABASE_API_KEY}`
  (GM-987; the key is not deployed yet and the job that uses it is off by default).
  .NET does not expand these; they only document that the value is injected.
- Deployed values come from argocd (`../argocd/apps/corp-apps/<service>/<env>.yaml`): ExternalSecrets
  from AWS SSM Parameter Store for the connection strings, JWT key, RabbitMQ credentials and the
  Metabase SSO key; plain `env:` entries for the API keys and the Azure Storage connection string.
- The unused `Sportsbook:PlatformApi` block was removed from `Backoffice.Web/appsettings.json`
  (nothing reads it).
- Still open: the old values are in git history and must be rotated (stg/prd RDS `admin`, the old
  `sa` password, RabbitMQ `fcadmin`, the Azure `prodstoragecdn` account key, the API keys, the JWT
  key and the Metabase SSO key). The argocd `env:` blocks also carry API keys and the Azure Storage
  key in plaintext and should move to ExternalSecrets.
- Local runs: supply real values through environment variables (see README "Running locally").

**Related pattern to watch for:** a sibling repo (platform-final, GM-880, 2026-09-30) found a
different but adjacent leak, the OG private API key was printed in plaintext via `_logger.LogError`
on every failed bet placement, visible in PRD Loki logs. Not found in this repo as of 2026-10-02,
but applies equally here: never pass a secret/credential value as a log template argument, mask it
first (keep only the last few characters, blank the rest). See `CLAUDE.md` Safety rules → Secrets.

---

### 3. Missing Global Exception Handling

**Location:** `Backoffice.API/Startup.cs` - exception handler commented out
```csharp
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Error");  // COMMENTED OUT
    //app.UseHsts();
}
```

**Severity:** 🔴 CRITICAL

**Impact:**
- Unhandled exceptions cause 500 errors without details
- No centralized error logging
- Each repository implements own try-catch
- Inconsistent error responses

**Risk:**
- Errors lost in logs
- No audit trail for production issues
- Debugging difficult
- SLA violations

**Current Workaround:**
Each repository catches exceptions:
```csharp
catch (Exception ex)
{
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

⚠️ **Problem:** Errors logged at repository level, hard to correlate

---

### 4. API Key Validation Only - No Bearer Token Validation (corrected, BO-132)

The earlier description here (a Bearer token extracted but never validated in `ApiKeyFilter`) does not
match the code. `Backoffice.API/ActionFilter/ApiKeyFilter.cs` reads no `Authorization` header at all: it
only compares the `x-api-key` header with `Security:ApiKey` and rejects the request when they differ.

Bearer tokens are handled by `Backoffice.API/ActionFilter/AuthorizeAttribute.cs`, which checks the issuer
and expiry locally, then validates the token against the Authentication API (`Authenticate(token)`) and,
when a claim is required, checks the permission claim (see `09_Authentication.md`, BO-130). An action that
carries only `ApiKeyFilter` and no `[Authorize]` accepts any caller holding the shared API key, with no user
identity; that is the real gap to track per controller.

**Severity:** not critical as originally described. Remaining smaller issues in `AuthorizeAttribute`:
`async void OnAuthorization`, a new `HttpClient` per attribute instance, and the catch block returning
`ex.Message` to the caller.

---

### 5. CORS Allows Any Origin

**Location:** `Backoffice.API/Startup.cs` line 55-60
```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", options => 
        options.AllowAnyOrigin());  // No origin validation
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()    // Allows any domain
               .AllowAnyMethod()    // Allows all HTTP methods
               .AllowAnyHeader();   // Allows all headers
    });
});
```

**Severity:** 🔴 CRITICAL

**Impact:**
- Cross-Origin Resource Sharing (CORS) policy too permissive
- Any website can make requests to API
- No origin validation

**Risk:**
- Cross-Site Request Forgery (CSRF) attacks
- Data exfiltration from other domains
- Unauthorized API usage

---

## High Priority Issues (Fix in Next Sprint)

### 6. HttpClient Created Per Request

**Location:** `Backoffice.API/Repositories/RabbitMQRepository.cs` line 75-85
```csharp
private HttpClient GetHttpClient(string region)
{
    var uri = GetBaseUrl(region);
    if (uri.LastOrDefault() != '/')
        uri += "/";

    var client = new HttpClient()  // NEW INSTANCE CREATED
    {
        BaseAddress = new Uri($"{uri}")
    };
    return client;
}
```

**Severity:** 🟠 HIGH

**Impact:**
- New HttpClient instantiated on every call
- Not disposed (memory leak)
- Socket exhaustion risk
- Performance degradation

**Pattern Violations:**
- Best practice: Reuse HttpClient instances
- Breaks connection pooling
- Bypasses HttpClientFactory

**Frequency:** Every message publish

---

### 7. Fire-and-Forget Message Publishing

**Location:** `Backoffice.API/Repositories/PlatformRepository.cs` line 76+
```csharp
await _rabbitMqRepository.Publish(new MessageRequest
{
    Payload = JsonSerializer.Serialize(...),
    CorrelationId = Guid.NewGuid().ToString().ToUpper(),
    MessageType = "OnClientChange",
    RoutingKey = $"r.oe.cachesync.onclientchange"
}, region);
// Result not awaited or checked
```

**Severity:** 🟠 HIGH

**Impact:**
- Message publishing failure ignored
- Inconsistent state (DB updated, message not sent)
- Cache invalidation messages never delivered
- Silent failures

**Risk:**
- Data inconsistency across services
- Stale caches not invalidated
- No audit trail of failed publishes

---

### 8. MongoDB Static Connection (Thread-Safety)

**Location:** `Backoffice.API/Repositories/SportsbookRepository.cs` line 34-40
```csharp
private static IMongoDatabase _db;  // STATIC
private static IMongoCollection<SportLookup> _sportLookupCollection;

public SportsbookRepository(...)
{
    if (_db == null)  // Not thread-safe
    {
        var client = new MongoClient(...);
        _db = client.GetDatabase("Sportsbook");
        _sportLookupCollection = _db.GetCollection<SportLookup>("SportLookup");
    }
}
```

**Severity:** 🟠 HIGH

**Impact:**
- Static fields not thread-safe
- Multiple threads could initialize simultaneously
- Race condition on null check
- MongoDB client creation duplication

**Risk:**
- Unpredictable behavior under load
- Thread-safety not guaranteed by MongoDB driver
- Concurrency issues hard to reproduce

**Better Pattern:**
```csharp
private readonly MongoClient _mongoClient;
private readonly IMongoDatabase _db;
private readonly IMongoCollection<SportLookup> _sportLookupCollection;
```

---

### 9. Configuration not Validated on Startup

**Location:** `Backoffice.API/Startup.cs`
**Missing:** Configuration validation in ConfigureServices

**Severity:** 🟠 HIGH

**Impact:**
- Invalid configuration only discovered on first request
- No startup health checks
- Services start even if configuration is broken

**Examples of Unvalidated:**
```
- API endpoint URLs (invalid)
- API keys (empty, wrong format)
- Database connection strings (wrong server)
- RabbitMQ broker URI (unreachable)
```

**Risk:**
- Production deployment with broken configuration
- Service fails after startup
- Difficult to diagnose

---

### 10. Mixed JSON Serialization Libraries

**Location:** Multiple files
```csharp
// System.Text.Json
JsonSerializer.Serialize(object)

// Newtonsoft.Json (JsonConvert)
JsonConvert.DeserializeObject<T>()

// Both used in same codebase
```

**Severity:** 🟠 HIGH

**Impact:**
- Conflicting serialization rules
- DateTimeFormat differences
- Null handling inconsistencies
- Potential deserialization failures

**Example:** `PlatformRepository.cs` line 46+
```csharp
// Uses Newtonsoft.Json for response
return JsonConvert.DeserializeObject<BaseResponse<List<...>>>(
    await response.Content.ReadAsStringAsync());
```

vs `RabbitMQRepository.cs` line 37
```csharp
// Uses System.Text.Json for request
new StringContent(JsonSerializer.Serialize(request), ...)
```

---

### 11. DbContext Created Manually in Config.Service (resolved, BO-138)

**Location:** `Backoffice.Config.Service/Repository/BackofficeRepository.cs`, `Program.cs`

**Severity:** 🟠 HIGH (until BO-138)

**Impact (before the fix):** the singleton repository built one `BackofficeContext` in its constructor and kept it for
the life of the process. `DeleteConfigTemplate` wrapped it in `using (var context = _backofficeContext)`, so the first
template delete disposed it. Every later `r.bo.config.*` message (margin updates and deletes) then failed with
"Cannot access a disposed context instance". The worker logged the error and returned, so the message broker acked
the message and it was lost, while the service looked healthy until a restart.

**Fix (BO-138):** `Program.cs` registers `AddDbContextFactory<BackofficeContext>` and the repository creates a
short-lived context per call (`using var context = _contextFactory.CreateDbContext()`). Injecting `BackofficeContext`
into the singleton, as this item used to suggest, would not have fixed it: the singleton would still hold one context
for its whole life. `UpdateClientMargin` no longer swallows its exceptions. `Worker` rethrows a failure on the first
delivery, so `MessageBroker.HandleMessage` nacks and requeues it for one retry; when the redelivered message fails
again it is logged as an error with its payload and acked, so a bad message cannot loop forever. Covered by
`Backoffice.Config.Service.Tests`.

**Still open:** the service has no health check (it is a worker with no HTTP endpoint), so it cannot report unhealthy
when the database is unreachable; failures are visible only as errors in the bo-config-service log. The queue has no
dead-letter exchange, so a message that fails twice is dropped after the error log.

---

### 12. Inconsistent Error Handling Patterns

**Location:** Multiple repositories

**Severity:** 🟠 HIGH

**Pattern Inconsistency:**

Repository A:
```csharp
try { ... }
catch (Exception ex) 
{ 
    _logger.LogError("Error: {ex}", ex);
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

Repository B:
```csharp
try { ... }
catch (Exception ex) 
{ 
    return new BaseResponse { StatusCode = StatusCode.Error, Message = ex.Message };
}
```

Repository C:
```csharp
try { ... }
catch (Exception ex) 
{ 
    // No logging
    return new BaseResponse { StatusCode = StatusCode.Error };
}
```

**Impact:**
- Inconsistent error reporting
- Some errors logged, others silent
- Difficult error diagnosis
- No standardized error responses

---

### 13. Background Service Repository Registered as Singleton (resolved, BO-138)

**Location:** `Backoffice.Config.Service/Program.cs`
```csharp
services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
```

The repository is still a singleton, but since BO-138 it holds only a `IDbContextFactory<BackofficeContext>` and
creates a new context for each message, so no context or change-tracking state is shared between messages. See #11.

**Still open:** `Backoffice.Utility/Repository/BackofficeRepository.cs` (also a singleton) still keeps one
`BackofficeContext` field for the life of the process, next to methods that create their own context.

---

### 14. Activity Log Marked for Removal But Still Used

**Location:** `Backoffice.API/Repositories/BackOfficeRepository.cs` line 65
```csharp
// Note: Will Remove
public async Task<BaseResponse<bool>> AddActivityLog(ActivityLogModel request)
```

**Severity:** 🟠 HIGH

**Impact:**
- Dead code path (or is it?)
- Unclear if still needed
- Could be removed incorrectly
- Technical uncertainty

**Issues:**
- Function still called from controllers
- Deprecation not communicated
- Migration plan unclear

---

### 15. Hardcoded Timeouts and Configuration

**Location:** `Backoffice.Web/Program.cs` line 29
```csharp
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(o => o.DetailedErrors = true);
```

**Also:** `Backoffice.WebSocket.API/Program.cs` line 29
```csharp
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => 
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3));
```

**Severity:** 🟠 HIGH

**Impact:**
- Circuit configuration not parameterized
- Can't change without recompiling
- 3-minute retention may not suit production load

---

## Medium Priority Issues (Schedule for Later)

### 16. No API Rate Limiting

**Severity:** 🟡 MEDIUM

**Impact:**
- No protection against brute force attacks
- No DDoS protection
- No API usage limits
- No SLA enforcement

---

### 17. TODO Comments in Code

**Location:** Multiple locations
```csharp
// Web/Program.cs: "// DPK TODO: configure dpk"
// API/Repositories/BackOfficeRepository.cs: "//TODO: get username from Authentication Repository"
```

**Severity:** 🟡 MEDIUM

**Impact:**
- Incomplete implementation
- Unclear requirements
- Technical debt marker
- Code quality indicator

---

### 18. Partial HttpClient Configuration — ✅ RESOLVED (BO-2, 2026-08)

**Location:** `Backoffice.API/Startup.cs` (Maintenance HttpClient registration)

The `x-api-key` header for the Maintenance HttpClient was set to the string literal
`"API:Maintenance:Security:ApiKey"` instead of `Configuration["API:Maintenance:Security:ApiKey"]`,
so every Dapr maintenance call was sent with a garbage key. Fixed under BO-2 by reading the
value from configuration.

**Severity:** 🟡 MEDIUM (historical)

---

### 19. Logging Configuration Inconsistency

**Severity:** 🟡 MEDIUM

**Observations:**
- API uses Startup.cs pattern
- Web uses Minimal APIs pattern
- Different logging setup approaches
- Configuration sources differ

**Impact:**
- Inconsistent logging behavior
- Harder to maintain
- Team confusion on patterns

---

### 20. No Dependency Validation

**Severity:** 🟡 MEDIUM

**Missing Checks:**
- ✗ Database connectivity
- ✗ RabbitMQ availability
- ✗ External API reachability
- ✗ Configuration completeness

**Impact:**
- Services start but fail immediately
- Hard to diagnose deployment issues
- No pre-flight validation

---

### 21. Complex Repository Classes

**Severity:** 🟡 MEDIUM

**Example:** `BackofficeRepository.cs`
- 2000+ lines of code
- Dozens of methods
- Multiple responsibilities
- Hard to test

**Violation:** Single Responsibility Principle

**Impact:**
- Difficult to understand
- Slow to test
- High maintenance burden

---

### 22. No Request/Response Validation

**Severity:** 🟡 MEDIUM

**Missing:**
```csharp
// No attribute-based validation
[Required]
[Range(0, 100)]
[EmailAddress]

// No FluentValidation
// No manual validation in controllers
```

**Impact:**
- Invalid data accepted
- Business logic must handle edge cases
- Data quality issues

---

### 23. Inconsistent Async Patterns

**Severity:** 🟡 MEDIUM

**Observations:**
- Config.Service uses no async database access
- API uses async throughout
- Some methods don't await properly

---

### 24. No Documentation

**Severity:** 🟡 MEDIUM

**Missing:**
- ✗ API endpoint documentation (XML comments)
- ✗ Database schema documentation
- ✗ Message format documentation
- ✗ Configuration documentation
- ✗ Deployment procedures

---

### 25. Incomplete Reference Handling

**Severity:** 🟡 MEDIUM

**In BackofficeContext:**
```csharp
modelBuilder.Entity<ClientInformation>()
    .HasKey(k => new { k.ClientId });

// No navigation properties
// No foreign keys defined
// No relationship configuration
```

---

## Low Priority Issues (Backlog)

### 26. SignalR User Tracking Not Production-Ready

**Location:** `BackofficeHub.cs`
```csharp
private static readonly Dictionary<string, string> _users = new();

public override Task OnConnectedAsync()
{
    var userId = Context.GetHttpContext()?.Request.Query["userId"];
    if (!string.IsNullOrEmpty(userId))
    {
        lock (_users)  // Simple lock, not scalable
        {
            _users[userId] = Context.ConnectionId;
        }
    }
}
```

**Issues:**
- ✗ Not distributed (single server only)
- ✗ Dictionary cleared on app restart
- ✗ Stale entries not cleaned up
- ✗ No cleanup on connection error

**Security (BO-131, October 29, 2026):** the hub accepted every connection with no credential, any client could call the `Publish*` methods (fake bet stops, odds changes and notification pop-ups to every Back Office screen), and the user id came unverified from `?userId=`, so a second connection could take over a user's notifications. Fixed in `Backoffice.WebSocket.API/Security/`: callers send `x-api-key` (`Security:PublisherApiKey` for WebSocket.Service, `Security:SubscriberApiKey` for the Web and Utility), the hub requires a key to connect and the publisher key to call any `Publish*` method, and the user id comes from the Auth JWT the Web sends as a Bearer token (validated locally with `API:AuthenticationAPI:JWT:Key`/`Issuer`). A notification addressed to a user who is not connected is now dropped; it used to fall through to `Clients.All`, because WebSocket.Service always sends `Gr = null`. `Security:Enforce=false` only logs callers that would be refused, for the rollout (clients first, then turn it on); `true` with a missing key or JWT setting stops the hub at startup. Tests: `Backoffice.WebSocket.API.Tests`. The scalability issues above are unchanged.

**Severity:** 🟢 LOW (Low immediate risk, but limits scalability)

---

### 27. Health Checks Are Basic

**Location:** `Startup.cs`
```csharp
services.AddHealthChecks();
```

**Missing:**
- Database connectivity check
- RabbitMQ availability check
- External API reachability

---

### 28. No Distributed Tracing Configuration Details

**Severity:** 🟢 LOW

**Observations:**
- OpenTelemetry registered
- ActivitySource used inconsistently
- No exporter configured (implicit)
- No sampling strategy

---

### 29. Configuration Comments Outdated

**Location:** `appsettings.json`
```json
// "Backoffice": "Data Source=127.0.0.1,1433;..."  // Commented out
// "Backoffice": "Data Source=prd-song88-mssql-server..."  // Commented out
```

**Impact:** Confusion about which connection string is active

---

### 30. No Performance Baseline

**Severity:** 🟢 LOW

**Missing:**
- Response time baselines
- Memory usage benchmarks
- Database query performance metrics

---

## Issue Summary Table

| # | Issue | Severity | Category | Impact |
|---|-------|----------|----------|--------|
| 1 | DevEx page always enabled | 🔴 CRITICAL | Security | Information disclosure |
| 2 | DB credentials in config | 🔴 CRITICAL | Security | Data breach risk |
| 3 | No global exception handler | 🔴 CRITICAL | Ops | Lost errors |
| 4 | Bearer token not validated | 🔴 CRITICAL | Security | Auth bypass |
| 5 | CORS allows any origin | 🔴 CRITICAL | Security | CSRF/CORS attacks |
| 6 | HttpClient per request | 🟠 HIGH | Performance | Memory leak |
| 7 | Fire & forget publishing | 🟠 HIGH | Reliability | Data inconsistency |
| 8 | MongoDB static (unsafe) | 🟠 HIGH | Concurrency | Race conditions |
| 9 | No config validation | 🟠 HIGH | Ops | Deployment failures |
| 10 | Mixed JSON libraries | 🟠 HIGH | Bugs | Deserialization errors |
| 11 | Manual DbContext creation | 🟠 HIGH | Architecture | Testability |
| 12 | Inconsistent error handling | 🟠 HIGH | Code Quality | Hard to debug |
| 13 | Repository as singleton | 🟠 HIGH | Architecture | Memory leaks |
| 14 | Dead code marked for removal | 🟠 HIGH | Code Quality | Uncertainty |
| 15 | Hardcoded config | 🟠 HIGH | Flexibility | Can't reconfigure |
| 16 | No rate limiting | 🟡 MEDIUM | Security | Attack vector |
| 17 | TODO comments | 🟡 MEDIUM | Code Quality | Incomplete |
| 18 | Partial config | 🟡 MEDIUM | Bugs | API key wrong |
| 19 | Logging inconsistency | 🟡 MEDIUM | Maintenance | Confusion |
| 20 | No dependency validation | 🟡 MEDIUM | Ops | Deployment issues |
| 21 | Large repository classes | 🟡 MEDIUM | Maintenance | Hard to test |
| 22 | No data validation | 🟡 MEDIUM | Quality | Bad data |
| 23 | Inconsistent async | 🟡 MEDIUM | Bugs | Hangs/deadlocks |
| 24 | No documentation | 🟡 MEDIUM | Maintenance | Hard to use |
| 25 | Incomplete refs | 🟡 MEDIUM | Architecture | N/A queries |
| 26 | SignalR user tracking | 🟢 LOW | Scalability | Single server only (auth fixed in BO-131) |
| 27 | Basic health checks | 🟢 LOW | Ops | Limited insight |
| 28 | No tracing config | 🟢 LOW | Observability | Missing data |
| 29 | Outdated comments | 🟢 LOW | Maintenance | Confusion |
| 30 | No perf baseline | 🟢 LOW | Monitoring | No SLAs |

---

## Recommended Remediation Timeline

### Week 1 (Critical)
1. Fix database credentials (move to secrets manager)
2. Enable global exception handler
3. Fix Bearer token validation
4. Restrict CORS policy
5. Disable DevEx page in production

### Week 2-3 (High Priority)
1. Fix HttpClient creation pattern
2. Add message publishing error handling
3. Make MongoDB connection thread-safe
4. Add startup configuration validation
5. Standardize JSON serialization

### Month 2 (Medium Priority)
1. Standardize error handling
2. Add rate limiting
3. Improve data validation
4. Create API documentation
5. Establish deployment checklists

### Quarter 2 (Low Priority)
1. Refactor large repository classes
2. Improve health checks
3. Optimize database queries
4. Add performance baselines
5. Distribute SignalR tracking

---

## .NET 10 Upgrade (GM-743) — Resolved Framework EOL + Remaining Package Debt

**Date noted:** August 20, 2026

**Resolved:** All 9 projects (7 production + 2 test) upgraded from mixed targets (netcoreapp3.1 / net7.0 / net8.0) to `net10.0`, with framework-aligned packages bumped to 10.0.11 (EF Core, SignalR clients, MessagePack protocol, Mvc.NewtonsoftJson, System.Runtime.Caching, Configuration.Binder). Dockerfiles build/run on `sdk:10.0`/`aspnet:10.0`; CI test job uses `dotnet-version: 10.0.x` (the libssl1.1 workaround for the 3.1 test host was removed). Code changes were limited to `Backoffice.API/Repositories/MaintenanceRepository.cs`, which had accidentally used the `Join` extension from `Microsoft.EntityFrameworkCore.Internal` (removed in newer EF) — replaced with `string.Join`. `System.Text.Json` package reference removed from Shared (framework-provided on net10). bunit bumped 1.26.64 → 1.40.0 (1.26 depended on `IJSUnmarshalledRuntime`, removed from Microsoft.JSInterop in .NET 10).

**NuGet vulnerability follow-up:** the SignalR client, client core and MessagePack protocol packages
moved to 10.0.13 (GHSA-334f-vvp4-7g6p, GHSA-86xp-94pg-8r6r; this also lifts MessagePack to 2.5.303,
GHSA-qhrr-8q5h-9q3h). `System.Security.Cryptography.Pkcs` is pinned to 9.0.21 in BackOffice.API,
Config.Service and Utility over the 9.0.11 that Microsoft.Data.SqlClient 6.1.x pulls in
(GHSA-846f-f764-76mx; even SqlClient 6.1.7 still depends on 9.0.11). Remove the pin once SqlClient
depends on a patched version.

**Remaining debt (out of scope for GM-743, each needs its own coordinated change):**

| Package | Version | Where | Issue |
|---------|---------|-------|-------|
| RabbitMQ.Client | 5.2.0 | Shared, Web | Legacy major; 6.x/7.x have breaking API changes |
| WindowsAzure.Storage | 9.3.3 | Utility | Deprecated; successor is Azure.Storage.Blobs |
| EPPlus | 5.8.2 | Utility | Legacy 5.x. Its transitive System.Drawing.Common 5.0.0 (NU1904 critical advisory GHSA-rxg9-xrhp-64gj) is overridden by a direct 5.0.3 reference (BO-116); keep it on 5.0.x because 6.x+ throws on Linux |
| MongoDB.Driver | 3.12.0 | Shared | Resolved (BO-117/BO-118): upgraded from 3.7.1, which pulled SharpCompress 0.30.1 (NU1902) and Snappier 1.0.0 (NU1903 high, GHSA-pggp-6c3x-2xmx); 3.12.0 brings Snappier 1.3.1 and SharpCompress 0.48.1 |
| System.IdentityModel.Tokens.Jwt | 7.7.1 | Web | Resolved (BO-119): upgraded from 6.26.0 (NU1902 advisory GHSA-59j7-ghrg-fj52), matching the 7.7.1 the other projects already resolve. No code change needed. Every patched version (6.34.0+) rejects HS256 keys under 32 bytes with IDX10720, so `Metabase:SsoSecretKey` must be at least 32 bytes |
| Infra.Extensions.Telemetry | 0.1.1-5 | all services | Resolved: 0.1.1-5 brings OpenTelemetry 1.15.3, with no audit warnings (was 0.0.1-1, which pinned OpenTelemetry 1.14.0 with multiple NU1902 advisories) |
| OpenTelemetry.Api | 1.15.3 | Shared | Resolved (BO-121): direct reference upgraded from 1.5.0 (NU1902 advisory GHSA-g94r-2vxg-569j). The services already resolved 1.15.3 through Infra.Extensions.Telemetry, so there is no runtime change |
| Swashbuckle.AspNetCore | 6.4.0 | API | Old but functional on net10; upgrade when convenient |
| bunit | 2.11.3 | Web.Tests | Resolved (BO-120): upgraded from 1.40.0, which pulled AngleSharp 1.2.0 (NU1902 advisory GHSA-pgww-w46g-26qg). A direct AngleSharp override does not work: bunit 1.x is binary-incompatible with AngleSharp 1.5.0+ (`MissingMethodException` on `IHtmlCollection<T>.get_Item`). bunit 2 brings AngleSharp 1.8.1; tests use `BunitTestContext` for async disposal under xUnit 2.x |

These NuGet audit warnings (NU1902/1903/1904) are surfaced by the .NET 10 SDK during restore; they existed as transitive dependencies before the upgrade but were not audited by the old SDKs. None fail the build.

**Also noted during the upgrade (pre-existing, unrelated):** real Staging SQL credentials are committed in `Backoffice.API/appsettings.json` (`ConnectionStrings` block, including a commented-out Production string) — violates CLAUDE.md rule 6 (Connection String & Secret Safety); needs a credential rotation + move to git-ignored overrides, tracked separately.

## Excel exports moved out of the Utility service (BO-136)

**Date noted:** November 5, 2026

From 2024-11-29 (commit d6f25c1a, "Task - Migrate to api call") until BO-136, five Export buttons
showed "Success! Excel file is being generated" while the `r.bo.util.export` publish was commented
out, so no file was ever produced.

**Now:** Cancel Bet History, Manual Bet Settlement, Market Settlement and Message Settlement load
every row for the filters on screen (the activity log API returns all rows when `MetaData.CurrentPage`
is 0) and download a CSV at once (`Backoffice.Web/Services/HistoryExportCsvHelper.cs`, same BOM +
`CsvField.Escape` + `downloadFileFromText` pattern as the other exports). A failed request shows
"Export failed", not success. The file never leaves the user's session, so it needs no storage
link and nobody without the page's permission can download it. Cancel Bet History's table now also
applies its date range, as the export does.

**Removed:** Export on a Config template (My Templates). Utility only forwarded it to
`r.oe.web.config.ongetmarginspreadsheet3`, and nothing in any repo consumes that message, so there
was no file builder to switch back on. If the business wants a template export, it needs a new design.

**Remaining dead code (not reachable, left for a clean-up ticket):**
- `Backoffice.Utility` `Export*` cases (`Worker.cs`, `Repository/BackofficeRepository.cs`): EPPlus,
  `UploadToAzure` (not awaited, hard-coded production storage account, unsigned guessable links) and
  `SendAsync("SendMessageToUser")` on a hub without that method. Delete together with the
  `OnGet*HistoryMessage` / `OnGetOeConfigTemplateMessage` classes and the EPPlus and
  WindowsAzure.Storage packages.
- `HeaderConfigTopMenu` / `ImportExportMarginModal` (Import/Export Margins): no page renders them, and
  both the import and export publishes are commented out.
- The exports load all rows in one request and pass the CSV text through the Blazor circuit. That is
  fine for the date ranges used today; a very large range would need a streamed download from the API.
