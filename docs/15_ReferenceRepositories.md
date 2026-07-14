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

## See Also

- [14_SettlementFlows.md](14_SettlementFlows.md) — Full market settlement flow using these repos as reference
