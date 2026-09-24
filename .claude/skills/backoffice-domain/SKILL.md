---
name: backoffice-domain
description: Domain knowledge for the backoffice management layer: client administration, per-currency bet settings, config templates and margins, activity logging and Client Site Config. Use when working with BackofficeContext, BackOfficeRepository, ClientController, ConfigController or WebPropsController.
---

# BackOffice Domain

Domain knowledge for the backoffice management layer: client administration, per-currency bet settings, config templates and margins, activity logging, and Client Site Config. Apply this when working with `BackofficeContext`, `Backoffice.API/Repositories/BackOfficeRepository.cs`, `ClientController`, `ConfigController`, or `WebPropsController`. Paths are relative to `backoffice-final/`.

## Core entities in BackofficeContext

`Backoffice.Shared/Data/Backoffice/BackofficeContext.cs` (keys configured in `OnModelCreating`, `:10-22`):

| Entity | Purpose | Key fields |
|--------|---------|-----------|
| `ClientInformation` | A sportsbook client (operator brand) | `ClientId` (int), `Name`, `Brand` (the client code), `StatusId`, `ApplicationId`, `Region`, `IsDeleted`, plus fees, revenue shares, `TaxConfig` (JSON), `BetSlipMaxSelection` |
| `ClientBetSetting` | One row per client, currency and category | key `ClientBetSettingId`; `ClientId`, `CategoryId`, `Setting` (serialized string), `CurrencyCode` |
| `ConfigTemplate` | Named margin template | `Id`, `Name`, `SportId`, `FixtureTypeId`, `FixtureStatusId`, `Timestamp` |
| `ConfigTemplateMargin` | Per-market-template config inside a template | composite key `ConfigTemplateId, SportId, FixtureTypeId, FixtureStatusId, MarketTemplateId` (`BackofficeContext.cs:16`); `IsActive`, `Config` |
| `ConfigTemplateAssigned` | Template assignment per client/sport/segment/league | key `Id`; `ClientId` (Guid), `ClientCode`, `LeagueId`, `PrematchConfigTemplateId`, `LiveConfigTemplateId` |
| `ConfigTemplateAssignedMapping` | Links assignments to templates by fixture status | composite key `ConfigTemplateAssignedId, ConfigTemplateId, FixtureStatusId` |
| `ActivityLog` | Audit trail, table `ActivityLog2` (`Backoffice.Shared/Data/Backoffice/Entities/ActivityLog.cs:8`) | `Page`, `Payload`, `Activity`, `From`, `To`, `UserId`, `UserName`, `TimeStamp` |
| `ClientImportBatch`, `ClientImportBatchRow` | Bulk client import (`ClientImportController`) | `BatchId`, `Id` |

`ReadActivityLog` is a second `DbSet<ActivityLog>` used for a stored procedure (`BackofficeContext.cs:34-35`). There is no `ClientId` column on `ActivityLog`; the affected entity is encoded in `Payload` (e.g. `"{SportId}.{FixtureStatusId}.{Name}.T"` for config templates, `BackOfficeRepository.cs:1798`). A different `ActivityLog` class also exists in `Backoffice.Shared/Data/Trading/SQLEntities/`; do not confuse them.

## Client status and soft delete

`ClientStatus` has two values: `Normal = 100`, `Locked = 200` (`Backoffice.Shared/Enums/ClientStatus.cs:7-11`). There is no Pending, Active, Suspended, or Inactive state. Deletion is the separate `IsDeleted` flag.

Filter conventions in `BackOfficeRepository` (`Application2`: `SB = 2`, `SBC = 4`, `Backoffice.Shared/Enums/Application2.cs:12-20`):
- `GetClients` (dropdown list): `IsDeleted == false && StatusId == (int)ClientStatus.Normal && ApplicationId is SBC or SB` (`BackOfficeRepository.cs:378`).
- `GetClientList` (admin grid): `IsDeleted == false && ApplicationId is SBC or SB`, so Locked clients are included (`:411`). Both filter in memory: `GetClientInformation()` loads the whole table with `AsNoTracking().ToList()` (`:2320-2323`).

No non-test `.cs` file in this repo sets `IsDeleted` to true. Unverified: how clients get soft-deleted. `StatusId` also drives the platform side: `AddOrUpdateClient` sends `IsActive = request.StatusId == (int)ClientStatus.Normal` to the Platform API (`BackOfficeRepository.cs:1306`).

## Bet settings

Bet settings are per client, per currency, per `BetSettingCategory` (`Backoffice.Shared/Enums/BetSettingCategory.cs`): `Soccer = 1, OtherSports = 2, Parlay = 3, Games = 4, Virtual = 5, LiveCasino = 6, KenoLottery = 7, Cricket = 8, Priceboost = 9, BetBuilder = 10`. There is no per-market or per-league override hierarchy in `ClientBetSetting`.

`BackOfficeRepository.AddOrUpdateClient` (`:592`) saves them as part of the client save, inside a `TransactionScope` opened at `:1010`:
1. Validate (duplicate brand, duplicate name, website must contain `https://`; `#region VALIDATION` at `:599`).
2. Insert or update `ClientInformation`.
3. For each currency, build rows for Soccer, OtherSports, Parlay, Cricket, Priceboost, BetBuilder; delete the client's existing `ClientBetSetting` rows and insert the new set (`:1237-1243`).
4. Call Platform API `v1/client` for the client's `Region` (`PlatformRepository.cs:74-78`), which publishes `OnClientChange` on `r.oe.cachesync.onclientchange` with cache key `cc:{PlatformClientId}` upper-cased (`PlatformRepository.cs:86-97`).
5. Only if step 4 succeeded, push the bet config via `ProcessBackofficeClientBetConfig` with up to 3 attempts (`BackOfficeRepository.cs:1329-1340`).

A step 4 failure is logged with `Log.Error` and swallowed (`:1348`, `:1355`), and step 5 retries exhaust silently; either way `scope.Complete()` runs (`:1358`) and the method returns `Success`. `GetBetSetting` reads `ClientBetSetting` and deserializes `Setting` by `CategoryId` (`BackOfficeRepository.cs:1390-1477`).

## Activity logging

Activity logs are written synchronously to `ActivityLog2` by the API, not via RabbitMQ. Two API write paths exist:

- `BackOfficeRepository.AddActivityLog(ActivityLogModel)` and `AddActivityLogList` (`BackOfficeRepository.cs:68`, `:116`; the first is marked `// Note: Will Remove` at `:67`). Called internally (e.g. `DeleteConfigTemplate`, `:1815`) and exposed as `POST api/config/activitylog` and `POST api/config/set/activitylog/list` (`Backoffice.API/Controllers/Backoffice/ConfigController.cs:231`, `:244`), which `Backoffice.Web` calls after UI actions (`Backoffice.Web/Services/BackOfficeService.cs:349`, `:399`).
- `IBackofficeActivityLogRepository.ProcessActivityLog` (`Backoffice.API/Repositories/BackofficeActivityLogRepository.cs:19-43`), used by `TradingRepository`, `PlatformRepository`, `ClientImportRepository` (excerpt is `:29-42`):

```csharp
var logs = activityLogs.OnActivityLogMessages.Select(item => new ActivityLog
{
    Page = item.Page,
    Payload = item.Payload,
    Activity = item.Activity,
    From = item.From,
    To = item.To,
    UserId = item.UserId,
    UserName = item.UserName,
    TimeStamp = item.TimeStamp == DateTime.MinValue ? DateTime.UtcNow : item.TimeStamp
}).ToList();

_backofficeContext.ActivityLog.AddRange(logs);
await _backofficeContext.SaveChangesAsync();
```

`Activity` holds an `Activity` enum name: `Create, Update, Delete, Copy, ChangedPassword, ManualSettlement, PriceBoostChangedSelection, CancelBet` (`Backoffice.Shared/Enums/Activity.cs:7-17`). `Page` comes from the `x-page` header via `RequesterInfo` (`ConfigController.cs:111-116`) or from the UI.

`Backoffice.Utility` also consumes `LogActivity` and `LogActivityList` messages and writes the same table (`Backoffice.Utility/Worker.cs:81-88`, `Backoffice.Utility/Repository/BackofficeRepository.cs:48`). Unverified: who publishes those message types; nothing in `Backoffice.API` does, and a workspace grep finds no other publisher.

## Publishing messages from the API

The API does not use `RabbitMQ.Client` directly. `IRabbitMqRepository.Publish(MessageRequest request, string region = "hk")` (`Backoffice.API/Repositories/IRabbitMQRepository.cs:11`) POSTs to a broker HTTP API at `v1/broker/publish` (`RabbitMQRepository.cs:37`). `MessageRequest` has `Payload`, `MessageType`, `RoutingKey`, `CorrelationId` (`Backoffice.Shared/Models/Requests/MessageRequest.cs`). A real call (`BackOfficeRepository.cs:2102-2108`):

```csharp
await _rabbitMqRepository.Publish(new MessageRequest()
{
    CorrelationId = Guid.NewGuid().ToString(),
    MessageType = "UpdateClientMargin",
    RoutingKey = $"r.bo.config.updateclientmargin",
    Payload = JsonSerializer.Serialize(configTemplateId)
});
```

Publish returns a `BaseResponse<MessageResponse>` and catches its own exceptions (`RabbitMQRepository.cs:59-71`); no caller in `Repositories/` assigns the result, so publish failures are invisible.

## Config templates and margins

- `AddOrUpdateConfigTemplateMargin` (`BackOfficeRepository.cs:2058`) writes margins through the stored procedure `AddOrUpdateConfigTemplateMarginV2` (`:2100`) and then publishes `UpdateClientMargin` (above).
- `DeleteConfigTemplate` (`:1779`) removes the `ConfigTemplate`, publishes `DeleteConfigTemplate` on `r.bo.config.deleteconfigtemplate` (`:1808-1809`), and writes an activity log.
- `Backoffice.Config.Service` consumes both (`Backoffice.Config.Service/Worker.cs:58-65`). For `UpdateClientMargin` it builds per-client market configs from template assignments and publishes `OnUpdateClientMarketConfig` on `r.oe.web.config.OnUpdateClientMarketConfig` (`Backoffice.Config.Service/Repository/BackofficeRepository.cs:40`, `:90`).
- Assigned templates are read with the stored procedure `GetAssignedConfigTemplates` (`BackOfficeRepository.cs:1531`).

## Region

`ClientInformation.Region` holds values such as `HK` and `SA` (`BackOfficeRepository.cs:566-572`, SA branch commented out). Region selects which downstream deployment is called; it is not a cache or exchange prefix:
- `RabbitMqRepository` uses `API:RabbitMqApi:SA:Uri` when region is `SA`, otherwise `API:RabbitMqApi:HK:Uri` (`RabbitMQRepository.cs:92-98`). Most `Publish` calls omit region and therefore go to HK.
- `PlatformRepository` does the same with `API:PlatformApi:{SA|HK}:Uri` (`PlatformRepository.cs:1016-1022`).
- Controllers read the `x-region` header and pass it down (80 reads: 72 in `TradingController`, 5 in `BetController`, 3 in `ClientController`). Several reads are hard-coded to HK, e.g. `_platformRepository.GetClients("HK")` (`BackOfficeRepository.cs:1500`).

## User authentication model

Backoffice users log in through `AuthenticationController`, which delegates to the Auth API via `AuthenticationRepository` (`v1/login`, `v1/authentication/access`, `v1/logout`). On each request the custom `AuthorizeAttribute` checks issuer and expiry, validates the token with the Auth API (`ActionFilter/AuthorizeAttribute.cs:95`), and copies claims into `HttpContext.Items` (`:126-130`): `UserToken`, `UserId`, `UserName`, `UserIdRef`, `ClientCodes`. Controllers usually read them from there, not from `User`, e.g. `HttpContext.Items["UserId"]?.ToString()` (`ConfigController.cs:108-109`).

Permissions are claims named after pages (e.g. `SBC.OE.Config`, `Admin.ClientSiteConfig`) whose value is an `Access` int: `NoAccess = 0, Edit = 1, View = 2` (`Backoffice.Shared/Enums/Access.cs`). There are no roles. `ClientCodes` is `ALL` or a comma list of brands that scopes the client grid; `ClientController.GetClientList` parses it straight from the JWT (`Backoffice.API/Controllers/SportsbookClients/ClientController.cs:47-56`). See `api-development.md` for the full filter flow.

## Client Site Config (webprops-api)

Formerly "Feature Flags" (renamed in GM-696, commit `5a4f60ff`). The UI page is `Backoffice.Web/Pages/Administration/ClientSiteConfig.razor(.cs)` at `/admin/client-site-config`; the API route is still `api/config/feature-flags` and the types are still `ClientFeatureFlags*`. `WebPropsController` is gated by `Admin.ClientSiteConfig` (View at class level, Edit on the POST; `WebPropsController.cs:13`, `:42`).

- Data lives in webprops-api, not in BackofficeContext. `WebPropsRepository` calls `GET` and `POST /api/v1/config/{clientCode}` (`Backoffice.API/Repositories/WebPropsRepository.cs:37`, `:148`).
- The `{clientCode}` value is the client's Platform GUID (`PlatformClientResponse.ClientId`), not the SBC brand code (`ClientSiteConfig.razor.cs:22-26`).
- webprops POST is a full replace, so the repository fetches the document (`:86`) and overwrites only edited fields via `JObject` (`:116-145`). A 404 with "no properties for client" creates a new document; any other failure aborts (`:92-114`, `IsClientConfigNotFound` at `:202-217`).
- Partial cashout is coerced: `features["PartialCashout"] = request.FullCashout && request.PartialCashout;` (`WebPropsRepository.cs:125`, GM-790). `GameTrackerProductNames` and `LayoutThemes` are left untouched when null (`:130-145`).
- After a successful save it publishes a signal-only `OnWebPropsChange` on `r.oe.cachesync.onwebpropschange.{clientcode}` (client code lower-cased, `:173-182`).
- The activity log for a save is written by the UI via `api/config/activitylog`, with `Page = "Admin.ClientSiteConfig"` and `Payload = "{userName}.{clientGuid}"` (`ClientSiteConfig.razor.cs:222-231`).

## Caching

`IMemCache` is registered as a singleton (`Backoffice.API/Startup.cs:124`) and injected into `SportsbookRepository`, `TradingRepository` and `BackOfficeRepository`, but only `SportsbookRepository` actually calls it (keys `tlo` and `sportLookUp-{langCode}`, `SportsbookRepository.cs:822`, `:1274`); the other two have only commented-out uses (e.g. `boci` at `BackOfficeRepository.cs:2333`). `BackOfficeRepository` does not cache clients or templates. Downstream caches are invalidated by publishing `OnDeleteCacheMessage` payloads, e.g. `OnClientChange` above or `ConfigController` `cachepreload` (`ConfigController.cs:301`, `BackOfficeRepository.cs:2545`), whose `CacheType` values are listed in `Backoffice.Shared/Enums/CacheType.cs`.

## BackOfficeRepository scale warning

`Backoffice.API/Repositories/BackOfficeRepository.cs` is 3789 lines. Its regions: `Methods - Activity Logs` (`:66`), `Methods - Client` (`:372`), `Config` (`:1515`), `Methods - Top Fixtures` (`:2205`), `Utilities` (`:2306`), `Translation` (`:2544`), `Private Methods` (`:3617`). Add new methods to the matching region and do not reorganize existing code.
