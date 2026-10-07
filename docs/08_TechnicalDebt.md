# Technical Debt Analysis

**Document Status:** Detailed Analysis (No Code Changes Made)  
**Date:** June 18, 2026

## Executive Summary

The Backoffice solution has **solid architectural foundations** but exhibits several categories of technical debt that should be addressed systematically. This analysis categorizes issues by severity and provides guidance for remediation.

---

## Authorization Coupling — Shared `activitylog/list` Endpoint

**Date noted:** June 25, 2026

**Issue:** The generic activity-log read endpoint `POST api/config/activitylog/list` lives in `ConfigController`, which is gated at the class level by `[Authorize("SBC.OE.Config", View)]` (`Backoffice.API/Controllers/Backoffice/ConfigController.cs:24`). Three unrelated features call this single endpoint:

| Caller | Permission the feature uses | Correctly gated? |
|--------|-----------------------------|------------------|
| OddsEngine Config template logs | `SBC.OE.Config` | ✅ yes |
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

## Critical Issues (Fix Immediately)

### 1. Developer Exception Page Always Enabled

**Location:** `Backoffice.Web/Program.cs` line 54
```csharp
// temp
app.UseDeveloperExceptionPage();  // ALWAYS ENABLED
```

**Severity:** 🔴 CRITICAL

**Impact:**
- Exposes sensitive information in production
- Stack traces, SQL queries, internal paths visible
- Security vulnerability (OWASP A01)

**Risk:**
- Information disclosure
- Detailed error information available to attackers
- Configuration secrets potentially leaked

**Frequency:** Every error in production

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

### 4. API Key Validation Only - No Bearer Token Validation

**Location:** `Backoffice.API/ActionFilter/ApiKeyFilter.cs` line 23-29
```csharp
var token = context.HttpContext.Request.Headers[HeaderNames.Authorization]
    .ToString().Replace("Bearer", "").Trim();

// Token extracted but NEVER VALIDATED
if (string.IsNullOrEmpty(apiKey) || ...)
{
    context.Result = new BadRequestObjectResult(...);
}
```

**Severity:** 🔴 CRITICAL

**Impact:**
- Bearer token extracted but not validated
- Any "Bearer {anything}" passes through
- JWT validation delegated to AuthorizeAttribute (inconsistent)
- Security bypass possible

**Risk:**
- Unauthorized access to protected endpoints
- Token validation inconsistency between endpoints
- Authentication bypass

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

### 11. DbContext Created Manually in Config.Service

**Location:** `Backoffice.Config.Service/Repository/BackofficeRepository.cs` line 29-32
```csharp
public BackofficeRepository(IConfiguration configuration, ILogger<BackofficeRepository> logger)
{
    var optionsBuilder = new DbContextOptionsBuilder<BackofficeContext>()
        .UseSqlServer(_configuration["ConnectionStrings:Backoffice"]);
    _backofficeContext = new BackofficeContext(optionsBuilder.Options);
}
```

**Severity:** 🟠 HIGH

**Impact:**
- Bypasses dependency injection
- Cannot mock/test easily
- DbContextPool not utilized
- Manual connection management

**Better Pattern:**
```csharp
public BackofficeRepository(BackofficeContext context) 
{
    _backofficeContext = context;
}
```

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

### 13. Background Service Repository Registered as Singleton

**Location:** `Backoffice.Config.Service/Program.cs` line 37
```csharp
services.AddSingleton<IBackofficeRepository, BackofficeRepository>();
```

**Severity:** 🟠 HIGH

**Impact:**
- Repository shared across all background task iterations
- DbContext created once, reused for lifetime
- No context per request pattern
- Memory leaks possible (context not disposed between messages)

**Risk:**
- Memory accumulation
- Change tracking state pollution
- Unexpected behavior with second and subsequent messages

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
| 26 | SignalR user tracking | 🟢 LOW | Scalability | Single server only |
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

**Remaining debt (out of scope for GM-743, each needs its own coordinated change):**

| Package | Version | Where | Issue |
|---------|---------|-------|-------|
| RabbitMQ.Client | 5.2.0 | Shared, Web | Legacy major; 6.x/7.x have breaking API changes |
| WindowsAzure.Storage | 9.3.3 | Utility | Deprecated; successor is Azure.Storage.Blobs |
| EPPlus | 5.8.2 | Utility | Legacy 5.x. Its transitive System.Drawing.Common 5.0.0 (NU1904 critical advisory GHSA-rxg9-xrhp-64gj) is overridden by a direct 5.0.3 reference (BO-116); keep it on 5.0.x because 6.x+ throws on Linux |
| MongoDB.Driver | 3.7.1 | Shared | Pulls SharpCompress 0.30.1 (NU1902) and Snappier 1.0.0 (NU1903 high, GHSA-pggp-6c3x-2xmx) |
| System.IdentityModel.Tokens.Jwt | 6.26.0 | Web | NU1902 advisory GHSA-59j7-ghrg-fj52; 7.x/8.x renames namespaces |
| Infra.Extensions.Telemetry | 0.0.1-1 | all services | Pins OpenTelemetry 1.14.0 transitively (multiple NU1902 advisories); needs a new package release |
| Swashbuckle.AspNetCore | 6.4.0 | API | Old but functional on net10; upgrade when convenient |

These NuGet audit warnings (NU1902/1903/1904) are surfaced by the .NET 10 SDK during restore; they existed as transitive dependencies before the upgrade but were not audited by the old SDKs. None fail the build.

**Also noted during the upgrade (pre-existing, unrelated):** real Staging SQL credentials are committed in `Backoffice.API/appsettings.json` (`ConnectionStrings` block, including a commented-out Production string) — violates CLAUDE.md rule 6 (Connection String & Secret Safety); needs a credential rotation + move to git-ignored overrides, tracked separately.

