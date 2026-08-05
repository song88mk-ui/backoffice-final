# Related Repositories

**Document Status:** Active Reference  
**Date:** June 19, 2026

---

## Repository Structure

All related service repositories live as siblings alongside `backoffice-final` in the same parent directory:

```
<repositories-root>/
├── Auth/                  ← Authentication service
├── backoffice-final/      ← CURRENT WORKING REPO (this solution)
├── bet-genius/            ← BetGenius integration
├── odds-engine-2/         ← OddsEngine (current, active)
├── oddsengine/            ← OddsEngine (old — reference baseline only, not active)
├── oddsfeed/              ← OddsFeed settlement service
├── platform-final/        ← Platform services (current, active)
├── platform-web/          ← Platform web frontend
├── webprops-api/          ← Web Properties API (frontend feature flags, themes, layouts, maintenance)
├── argocd/                ← Deployment / infrastructure configs
└── Reference/             ← Snapshot baseline taken before AI assistance (do not use for cross-service comparison)
```

> **Important:** `Reference/` is a frozen baseline snapshot from before AI-assisted development began. Always use the sibling repos directly (e.g. `../oddsfeed/`) — not the `Reference/` copies — when reading cross-service code.

> **`oddsengine/`** is the old OddsEngine. The current active version is **`odds-engine-2/`**.

---

## Repository Map

| Folder | What It Is | Status |
|--------|-----------|--------|
| `../Auth/` | Authentication service — JWT issuance, identity | Active |
| `../odds-engine-2/` | OddsEngine v2 | Active (current) |
| `../oddsengine/` | OddsEngine v1 | Reference baseline for odds-engine-2 only |
| `../oddsfeed/` | OddsFeed — bet settlement, placement, cancellation | Active |
| `../platform-final/` | Platform — settlement, operator, sportsbook, wallet | Active |
| `../platform-web/` | Platform web frontend | Active |
| `../bet-genius/` | BetGenius integration | Active |
| `../webprops-api/` | Web Properties API — per-client feature flags/themes/layouts/maintenance, Go, S3-backed | Active |
| `../argocd/` | Kubernetes / ArgoCD deployment configs | Active |
| `../Reference/` | Frozen AI baseline snapshot | Do not use |

---

## OddsEngine (odds-engine-2)

**Root:** `../odds-engine-2/src/OddsEngine/`

| Project | Role |
|---------|------|
| `OE.API` | OddsEngine REST API |
| `OE.Adapter.Service` | Processes UOF feed: `ON_FIXTURE_CHANGE`, `ON_MARKET_CHANGE`, `ON_MARKET_SETTLEMENT`, `ON_MARKET_CANCEL`, `ON_BET_STOP_*` |
| `OE.Sportsbook.Service` | Mirrors Adapter; handles fixture/market/settlement changes for sportsbook state |
| `OE.UOF.Client` | UOF (Betradar) feed client — connects to external broker; processes the raw UOF protocol feed |
| `OE.Job.Settlement` | Periodic job — runs every 5 minutes via `ProcessSettlement()` |
| `OE.Shared` | Shared models, constants (`MessageType`), utilities |

**Note:** No OddsEngine service handles `OnBetSettlement` from Backoffice directly — OE only processes raw UOF feed events (`ON_*` message types).

---

## OddsFeed (oddsfeed)

**Root:** `../oddsfeed/`

| Project | Role |
|---------|------|
| `OF.Settlement.Service` | **Primary bet settlement processor** — handles `OnBetSettlement`, `OnBetCancellation`, `OnBetBackOfficeManualSettlement`, `OnOddsFeedBetCancellation`. Contains `ResettleBet()` logic. |
| `OF.Operator.Service` | Operator-level processing |
| `OF.Placement.Service` | Bet placement |
| `OF.Cancellation.Service` | Bet cancellation |
| `OF.Job.Resettlement` | Resettlement scheduled job |
| `OF.Recovery.Service` | Recovery/retry service |
| `OF.Shared` | Shared models including `OnUOFBetSettlement`, `OnBetSettlement` |

---

## Platform (platform-final)

**Root:** `../platform-final/Platform/`

| Project | Role |
|---------|------|
| `Platform.Settlement.Service` | Consumes `OnBetSettlement` → `ProcessBetSettlement()` — updates bet status in platform DB |
| `Platform.Operator.Servicev2` | Operator settlement — handles `OnBetSettlement`, `OnBetSettlementOG`, `OnBetCancellation` |
| `Platform.BetForwarder.Service` | Forwards bet events downstream |
| `Platform.CashOutStake.Servicev2` | CashOut stake processing — also handles `OnBetSettlement` |
| `Platform.Sportsbook.API` | Sportsbook data API — **publishes** `OnBetSettlement` to `r.uof.onbetsettlement` |
| `Platform.Shared` | Shared models: `OnUOFBetSettlement`, `OnBetSettlement`, enums |
| `Platform.Job.Resettlement` | Resettlement scheduled job |

---

## Web Properties API (webprops-api)

**Root:** `../webprops-api/` (Go 1.23, module `s88/wp`, chi router)

Small Go HTTP API that serves per-client frontend configuration (feature flags, themes, layouts) and per-client maintenance flags to the Song88 frontend applications (platform-web, etc.). Storage is AWS S3 (bucket `song88-{env}-webprops`), with a shared in-process memory cache (TTL 86400s).

**Consumed by backoffice-final (two flows):**
1. **Feature Flags:** `Backoffice.API/Repositories/WebPropsRepository.cs` calls `GET/POST /api/v1/config/{clientId}` via an `IWebPropsRepository`, exposed through `WebPropsController` (`api/config/feature-flags/{clientCode}`, gated by the `Admin.ClientSiteConfig` claim — renamed from `Admin.FeatureFlags` in GM-693) and surfaced in `Backoffice.Web`'s "Client Site Config" admin page (`Pages/Administration/ClientSiteConfig.razor`, formerly the "Feature Flags" page).
2. **Maintenance (since BO-2, 2026-08):** `Backoffice.API/Repositories/MaintenanceRepository.cs` calls `GET/POST /api/v1/maintenance` with `X-Api-Key` + `X-Layout: {app}` per app in `Maintenance:Apps` (`el1, el2, el3, al, el4` — the frontend layout brands, mapping to S3 prefixes `EL1/…`). The `MaintenanceV2` admin page drives it. Set is **create-on-demand**: a client with no `{LAYOUT}/{CLIENTID}.json` object yet gets a stub created via `POST /api/v1/config/{clientCode}` (with `X-Layout`) before the flag is set — idempotent, existing objects are never replaced. The previous mechanism (Dapr-style invoke via the `bo-maintenance-api` router) is dead — that service is no longer deployed anywhere.

> **Consumer side (BO-2, 2026-08):** platform-web's `MaintenanceMiddleware` reads the flag from webprops `GET /api/v1/properties` (`X-Client-Id` + `X-Layout`), resolving the layout per request from the URL path (`/el2` → EL2, else AL — shared `LayoutResolver`, same rule as `_Host.cshtml`), cached per `{LAYOUT}-{CLIENTID}` for `Custom:MaintenanceCacheSeconds` (default 30s), fail-open on any webprops failure. The legacy local `Configs/maintenance.json` mechanism is removed. Because `web`, `og-web`, and `gamsnap-web` all deploy the same `platform-web` image, one image rollout covers all three. Layout codes actually resolvable by platform-web are **AL and EL2 only**; whether EL1/EL3/EL4 (in BO's `Maintenance:Apps`) are still served anywhere (e.g. `web-af`/ReactJS) is unconfirmed — trim the list once answered.

**Schema note:** webprops-api's config storage/API has no fixed schema — `get_config.go`/`update_config.go` decode/encode the whole document as a generic `map[string]interface{}`, so any key sent by a caller (Backoffice included) is persisted and returned as-is. The `Features` shape in the README/SKILL.md is illustrative convention, not an enforced contract. This means Backoffice can freely add new flags (e.g. `Features.BetRecommendation`, see `ClientFeatureFlags`) and they'll round-trip through S3 correctly with zero changes needed in webprops-api itself.

**Backoffice-only field:** `Features.BetRecommendation` (`Backoffice.Shared/Models/Response/ClientFeatureFlagsResponse.cs`) round-trips fine per the schema note above, but has no effect until `platform-web`'s own `Features` model and `Pages/Index.razor` (where `<BetRecommendation>` renders unconditionally today) are updated to read and gate on it.

| Endpoint | Auth | Purpose |
|---|---|---|
| `GET /api/v1/config/{clientId}` | none | Full client config (features, layout/theme options) |
| `POST /api/v1/config/{clientId}` | `X-Api-Key` | Full-replace update of a client's config JSON |
| `GET /api/v1/properties` | `X-Client-Id` header | Same config, read via header instead of path |
| `GET /api/v1/maintenance` | `X-Api-Key` + `X-Layout` | List maintenance flags for a layout |
| `POST /api/v1/maintenance` | `X-Api-Key` + `X-Layout` | Set maintenance flag for one/many/all clients in a layout |
| `POST /api/v1/cache/flush` / `DELETE /api/v1/cache/flush/{clientId}` | `X-Api-Key` | Cache invalidation |

Example config JSON (per client), from `../webprops-api/README.md`:

```json
{
  "ClientCode": "G01",
  "Features": {
    "BettingRules": true,
    "DarkMode": true,
    "GameTrackerProductNames": ["song88-light", "song88-dark"],
    "LanguageOptions": true,
    "OGAuth": false,
    "PartialCashout": true
  },
  "Layout": {
    "AL": { "Themes": ["red", "gray", "blue", "orange-light", "orange-dark"] },
    "EL2": { "Themes": ["red", "white"] }
  }
}
```

**Environments:** dev (public ingress at `platform-webprops.dev.song88.com`, ECR account 418541840646/ap-south-1), staging (`*-stg` release tag, account 172065883315/ap-south-1), production (`*-prd` release tag, account 525375333516/ap-east-1). ArgoCD app definitions: `../argocd/apps/platform/webprops-api/{dev,stg,prd}.yaml`.

**Full reference:** `../webprops-api/.claude/skills/webprops-api/SKILL.md` (endpoint table, storage/cache internals, known gotchas, deploy pipeline).

---

## See Also

- [14_SettlementFlows.md](14_SettlementFlows.md) — Full market settlement flow using these repos as reference
