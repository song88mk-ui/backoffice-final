# Settlement Flows

**Document Status:** Active Reference  
**Last Updated:** June 19, 2026  
**Based on:** Live code from backoffice-final, odds-engine-2, oddsfeed, platform-final, argocd

> **June 2026 update:** `OE.Adapter.Service` is being updated to subscribe to `r.uof.#` in addition to `r.bg.receiver.#`. This connects the Backoffice manual market settlement pipeline to the full UOF processing chain (MongoDB update + per-selection settlement fan-out). See §Architecture Diagram and §Confirmed Findings for details.

---

## Overview

Three settlement workflows exist in the Backoffice. All ultimately converge on the same downstream services.

| Flow | Entry Point | Audit DB | Downstream |
|------|------------|----------|-----------|
| **Market Settlement** | Market Settlement page | Backoffice DB (`ActivityLog2`) | `r.uof.onbetsettlement` → OE.Adapter.Service → `r.oe.bet.settlement` (requires OE.Adapter.Service `r.uof.#` subscription) |
| **Manual Bet Settlement** | Manual Bet Settlement page | Platform DB | Platform API (`v1/bet/manualsettlement`) |
| **Resend Settlement** | Resend Settlement page | Platform DB | Platform API (`v1/bet/resendsettlement`) |
| **Resend Cancellation to Bet Genius** (BO-125) | Resend Settlement page, second button | Platform DB | Platform API (`v1/bet/resendprovidercancellation`), see the section below |

---

## Two Parallel Settlement Pipelines

The platform runs two independent settlement pipelines that share the same downstream steps:

| | UOF Pipeline (Betradar feed) | Backoffice Manual Pipeline |
|--|-------------------------------|---------------------------|
| **Source** | Betradar live feed | Backoffice.Web UI |
| **Entry routing key** | `r.oe.adapter.*.{instance}` | `r.uof.onbetsettlement` |
| **OE processing** | OE.Adapter → OE.Sportsbook.Service | OE.Adapter.Service (once `r.uof.#` subscription is added) → OE.Sportsbook.Service |
| **Settlement routing key** | `r.oe.bet.settlement` | `r.oe.bet.settlement` (published by OE.Sportsbook.Service per selection) |
| **Consumers** | OF.Settlement.Service + Platform.Settlement.Service | OF.Settlement.Service + Platform.Settlement.Service |

> **`r.uof.onbetsettlement` requires `OE.Adapter.Service` subscription update:**
> - As of June 2026, `OE.Adapter.Service` subscribes only to `r.bg.receiver.#` (BetGenius only)
> - Adding `r.uof.#` to `OE.Adapter.Service`'s routing key pattern will complete the Backoffice pipeline
> - Once live, Backoffice.Web messages to `r.uof.onbetsettlement` will flow through OE.Adapter.Service → OE.Sportsbook.Service (MongoDB update) → `r.oe.bet.settlement` → settlement services
> - Until then, Backoffice publishes `OnBetSettlement` directly to `r.oe.bet.settlement` (bypassing MongoDB update)

---

## Architecture Diagram

```
═══════════════════════════════════════════════════════════════════
  UOF PIPELINE (Betradar → Bets)
═══════════════════════════════════════════════════════════════════

Betradar UOF Feed (external)
        │ raw feed
        ▼
OE.UOF.Client (odds-engine-2)
  Subscribes: r.oe.adapter.#.# | r.oe.uof.heartbeat | r.of.recovery.#.*
  Handles: ON_MARKET_SETTLEMENT → ProcessMarketSettlement()
  Publishes: onmarketsettlement → uof.onmarketsettlement.{sport} (External Broker)
  Also forwards: → r.oe.adapter.{type}.{instance}
        │
        ▼
OE.Adapter.Service (odds-engine-2)
  Subscribes: r.bg.receiver.* | r.uof.#  ← r.uof.# being added to accept Backoffice manual settlements
  Handles: ON_MARKET_SETTLEMENT → ProcessOnMarketSettlementMessage()
  Publishes: ON_MARKET_SETTLEMENT → r.oe.adapter.{messageType}.{instance}
        │
        ▼
OE.Sportsbook.Service (odds-engine-2)
  Subscribes: r.oe.adapter.#.01
  Handles: ON_MARKET_SETTLEMENT → ProcessOnMarketSettlementMessage()
  Action:  Updates Markets + Selections in MongoDB (bulk write)
  Publishes PER SELECTION: ON_BET_SETTLEMENT → r.oe.bet.settlement
           Payload: { MarketId, SelectionId, SelectionStatus, DeadHeatFactor }
        │
        ▼ r.oe.bet.settlement


═══════════════════════════════════════════════════════════════════
  BACKOFFICE MANUAL PIPELINE
═══════════════════════════════════════════════════════════════════

Backoffice.Web (User: Market Settlement page)
  Action: User selects fixture/market/selection, picks new status + reason
        │
        ▼
Backoffice.API  POST api/trading/manual/marketsettlement
  (TradingRepository.AddOrUpdateManualMarketSettlement)
  1. Validate: no duplicates, required fields, valid status, reason
  2. Write activity log directly → Backoffice DB `ActivityLog2`
     (BackofficeActivityLogRepository.ProcessActivityLog, Page = "Admin.Settlements.MarketSettlement").
     This row IS the audit trail AND the source for the Manual Market Settlement History list.
  3. Publish PER FIXTURE: OnBetSettlement → r.uof.onbetsettlement (see payload below)
  4. Return Success/Error
        │ (on Success, back in UI)
        ▼
Backoffice.API (TradingRepository) publishes PER FIXTURE: OnBetSettlement → r.uof.onbetsettlement
  Payload (OnBetSettlementMessage):
  {
    SportId, ProviderFixtureId, FixtureSpecifier,
    HomeScore: "", AwayScore: "",
    Selections: [{ MarketTemplateId, SelectionTemplateId, MarketSpecifier,
                   SelectionSpecifier, Result: bool, VoidFactor: enum, DeadHeatFactor }]
  }
        │
        ▼ r.uof.onbetsettlement
OE.Adapter.Service  (r.uof.# subscription being added)
  Handles: OnBetSettlement → ProcessOnMarketSettlementMessage()
  Publishes: → r.oe.adapter.onbetsettlement.{instance}
        │
        ▼
OE.Sportsbook.Service
  Handles: OnBetSettlement → ProcessOnMarketSettlementMessage()
  Action:  Updates Markets + Selections in MongoDB
  Publishes PER SELECTION: OnBetSettlement → r.oe.bet.settlement
  Payload (OnUOFBetSettlement):
  {
    SelectionId: long, SelectionStatus: BetStatus,
    DeadHeatFactor: decimal, SettlementScore: string
  }
        │
        ▼ r.oe.bet.settlement


═══════════════════════════════════════════════════════════════════
  SHARED DOWNSTREAM (both pipelines converge here)
═══════════════════════════════════════════════════════════════════

        ┌──────────────────────────────────────┐
        │  Subscribes: r.bet.settlement         │
        │            | r.oe.bet.settlement       │
        │            | r.of.bet.settlement       │
        │  Queue: q.of.bet.settlement            │
        ├──────────────────────────────────────┤
        │  OF.Settlement.Service (oddsfeed)     │
        │  Handles: OnBetSettlement             │
        │    → OnUOFBetSettlement               │
        │    → ProcessBetSettlement()           │
        │  DB: OddsFeed Betting DB              │
        │  Updates: Bet, BetSelection           │
        │  Creates: BetAudit, BetSelectionAudit │
        │  Logic: SettleBet() / ResettleBet()   │
        └──────────────────────────────────────┘
        │
        ├─ Publishes (currently DISABLED in OB): OnBetSettlement → r.bet.operator
        │
        ┌──────────────────────────────────────┐
        │  Subscribes: r.bet.settlement         │
        │            | r.oe.bet.settlement       │
        │            | r.of.bet.settlement       │
        │  Queue: q.bet.settlement               │
        ├──────────────────────────────────────┤
        │  Platform.Settlement.Service          │
        │  (platform-final)                     │
        │  Handles: OnBetSettlement             │
        │    → OnUOFBetSettlement               │
        │    → ProcessBetSettlement()           │
        │  DB: Platform Betting DB              │
        │  Updates: Bet, BetSelection           │
        │  Creates: BetAudit, BetSelectionAudit │
        │  Logic: SettleBet() / ResettleBet()   │
        └──────────────────────────────────────┘
        │
        ├─ Publishes: OnBetSettlement → r.bet.operator
        └─ Publishes: OnBetStatus → r.bet.betstatus
        │
        ▼ r.bet.operator
        ┌──────────────────────────────────────┐
        │  Subscribes: r.bet.operator           │
        │  Queue: q.bet.operator                │
        ├──────────────────────────────────────┤
        │  Platform.Operator.Servicev2          │
        │  (platform-final)                     │
        │  Handles: OnBetSettlement             │
        │    → ProcesssBetSettlement()          │
        │  Action: POST /v1/bet/settlement      │
        │           to operator HTTP endpoint   │
        └──────────────────────────────────────┘
        │
        ├─ On Success:
        │   Publishes: OnBetStatus → r.bet.betstatus.{clientCode}
        │   Publishes: OnWalletChange → r.wallet.{clientCode}
        └─ On Failure:
            RetryBetSettlement() with exponential backoff
```

---

## Resend Cancellation to Bet Genius (BO-125)

**Problem.** A bet cancelled at placement (the operator declined it or errored) was reported to the provider
(Bet Genius, "GTS" in platform-final) as Open when the placement was validated, and the Cancelled update never
followed. PRD bet `07AAA196-0010-499C-3E01-08DF245EDA48` was Cancelled in the Betting database and still open at
Bet Genius, who asked for the cancellation to be sent again. Resend Settlement cannot do this: the page refuses
Cancelled bets, and for a Cancelled bet the Platform API only replays the cancellation to the **operator**
(`PlatformRepository.ResendSettlement`, `cancelledBets` branch in platform-final).

**Flow.**

```
Resend Settlement page, "Resend Cancellation to Bet Genius" button -> ConfirmationModal (IsProviderCancellation)
  -> BackOfficeService.ResendProviderCancellation -> POST api/bet/resendprovidercancellation (BetController)
  -> PlatformRepository.ResendProviderCancellation -> Platform API POST v1/bet/resendprovidercancellation
       reads each bet from the Betting database, refuses any bet that is not Cancelled,
       builds an OnBetSettlement (IsAudit = true) and publishes it as OnBetCancellation on r.bet.cancellation
       (through Betting.API ResendSettlement, which now accepts an optional RoutingKey)
  -> Provider.Service OnBetCancellation handler: cancels the reservation, then SendBet with status Cancelled to GTS
```

**Rules.**
- The button is enabled only when every listed bet is loaded and Cancelled. A Reason is required, as for Resend
  Settlement (it is validated on the page and, as there, not sent). The request carries only the bet ids.
- The status shown on the page can lag the database when the bet also sits in the settlement retry queue (the
  details lookup then reads the queued message), so the Platform API re-reads the status and has the final say.
- It uses the existing `admin-settlement-resend` permission. No permission or menu entry was added.
- `r.bet.cancellation` is bound by `provider-service` only, whereas `r.bet.operator` is bound by both
  `operator-service` and `provider-service` (ArgoCD `apps/platform`). The provider-only key is what keeps a
  second cancellation from reaching the operator. Betting.API refuses any routing key other than these two.
- A success message means the cancellation was **queued**. `Provider.Service` delivers it to GTS and does not
  retry or report a failure back (`GTSBetProvider.ProcessSettlement`, `//TODO: Send to retry if failed`), so check
  that the bet now shows Cancelled at Bet Genius, or look at the `Provider.Service` logs.

**Deployment order.** platform-final Betting.API, then Platform.API, then bo-api, then bo-web. If Platform.API
goes out before Betting.API, the old Betting.API ignores `RoutingKey` and publishes on `r.bet.operator`, which also
delivers the cancellation to the operator. Rolling back is the reverse order, and an unused endpoint is harmless.

**Status.** Verified in code and by unit tests in both repos. Not observed against a real Bet Genius yet, and
whether GTS accepts a Cancelled update for a bet it holds as open is unknown. The message keeps `ChannelTypeId` at
its default, like the other resend paths, so a bet placed through the MTS channel is not told apart.

---

## Message Schemas

### OnBetSettlementMessage — Published by Backoffice.Web
**Source:** [Backoffice.Shared/Models/OnBetSettleMessage.cs](../Backoffice.Shared/Models/OnBetSettleMessage.cs)

```csharp
OnBetSettlementMessage {
    int SportId
    string ProviderFixtureId    // Betradar fixture ID
    string FixtureSpecifier     // e.g. "sr:match:12345"
    string HomeScore            // empty for manual
    string AwayScore            // empty for manual
    List<SelectionSettlementMessage> Selections
}

SelectionSettlementMessage {
    string MarketTemplateId
    string SelectionTemplateId
    string MarketSpecifier
    string SelectionSpecifier
    bool Result                 // true = Won/HalfWon, false = others
    VoidFactor? VoidFactor      // Zero / Half / One
    decimal DeadHeatFactor
}
```

### OnUOFBetSettlement — Consumed by OF.Settlement.Service & Platform.Settlement.Service
**Source:** `../oddsfeed/OF.Shared/MessageModels/OnUOFBetSettlement.cs`  
**Source:** `../platform-final/Platform/Platform.Shared/MessageModels/OnUOFBetSettlement.cs`

```csharp
OnUOFBetSettlement {
    long SelectionId            // resolved selection ID
    BetStatus SelectionStatus   // Won/Lost/Void/HalfWon/HalfLost
    string SettlementScore
    long StartTime              // Unix ms — for time-based filtering
    long EndTime
    decimal DeadHeatFactor
}
```

### OnBetSettlement — Emitted by Settlement Services to Operator
**Source:** `../platform-final/Platform/Platform.Shared/MessageModels/OnBetSettlement.cs`

```csharp
OnBetSettlement {
    Bet Bet
    List<BetSelection> BetSelections
    string Message
    bool IsRetry
    decimal NonTaxedPayout
    bool IsAudit
    bool IsResettle
}
```

---

## Status → OddsEngine Mapping (Backoffice UI)

Computed in [AddMarketSettlementModal.razor.cs](../Backoffice.Web/Components/Administration/MarketSettlement/AddMarketSettlementModal.razor.cs) before publishing:

| BetStatus | Result (bool) | VoidFactor |
|-----------|--------------|------------|
| Won | `true` | Zero |
| HalfWon | `true` | Half |
| Lost | `false` | Zero |
| HalfLost | `false` | Half |
| Void | `false` | One |

---

## Routing Key Reference

| Routing Key | Publisher | Subscriber | Purpose |
|------------|----------|-----------|---------|
| `r.uof.onbetsettlement` | Backoffice.Web, Platform.Sportsbook.API | OE.Adapter.Service (pending `r.uof.#` subscription) | Manual market settlement trigger — routed through OE pipeline once OE.Adapter.Service subscription is updated |
| `r.oe.adapter.{type}.{instance}` | OE.Adapter.Service | OE.Sportsbook.Service | Forward UOF events |
| `r.oe.bet.settlement` | OE.Sportsbook.Service | OF.Settlement.Service, Platform.Settlement.Service | Per-selection settlement |
| `r.bet.settlement` | — | OF.Settlement.Service, Platform.Settlement.Service | Settlement (general) |
| `r.of.bet.settlement` | — | OF.Settlement.Service, Platform.Settlement.Service | OddsFeed settlement |
| `r.bet.operator` | Platform.Settlement.Service | Platform.Operator.Servicev2 | Operator settlement notification |
| `r.bet.betstatus` | Platform.Settlement.Service | — | Bet status broadcast |
| `r.bet.betstatus.{clientCode}` | Platform.Operator.Servicev2 | — | Per-client status update |
| `r.wallet.{clientCode}` | Platform.Operator.Servicev2 | — | Wallet credit/debit |
| `r.oe.util.logs` | Backoffice.API | Backoffice.Utility | Internal activity log only |

---

## Key Files by Service

### Backoffice (backoffice-final)

| File | Role |
|------|------|
| [AddMarketSettlementModal.razor.cs](../Backoffice.Web/Components/Administration/MarketSettlement/AddMarketSettlementModal.razor.cs) | UI — collects settlement rows, calls the API (activity logging is no longer done client-side) |
| [TradingRepository.cs:1113](../Backoffice.API/Repositories/TradingRepository.cs#L1113) | API — validates, writes activity log to `ActivityLog2`, publishes `OnBetSettlement` per fixture |
| [BackofficeActivityLogRepository.cs](../Backoffice.API/Repositories/BackofficeActivityLogRepository.cs) | Writes activity-log rows directly to Backoffice DB `ActivityLog2` (no longer publishes to the broker) |
| [TradingController.cs](../Backoffice.API/Controllers/Trading/TradingController.cs) | Endpoint: `POST api/trading/manual/marketsettlement` |
| [OnBetSettleMessage.cs](../Backoffice.Shared/Models/OnBetSettleMessage.cs) | Message model sent to downstream |

### OddsEngine (odds-engine-2)

| File | Role |
|------|------|
| `../odds-engine-2/src/OddsEngine/OE.Sportsbook.Service/Repositories/SportsbookRepository.cs:127` | ProcessOnMarketSettlementMessage — updates MongoDB, publishes `r.oe.bet.settlement` |
| `../odds-engine-2/src/OddsEngine/OE.Adapter.Service/Worker.cs` | Subscribes `r.bg.receiver.*` + `r.uof.#` (being added), forwards UOF events |
| `../odds-engine-2/src/OddsEngine/OE.UOF.Client/Repository/ClientRepository.cs:218` | ProcessMarketSettlement — converts UOF to internal format |
| `../odds-engine-2/src/OddsEngine/OE.Shared/Constants/MessageType.cs` | All message type constants |

### OddsFeed (oddsfeed)

| File | Role |
|------|------|
| `../oddsfeed/OF.Settlement.Service/Worker.cs:101` | Consumes `r.oe.bet.settlement` etc., routes to handlers |
| `../oddsfeed/OF.Settlement.Service/Repository/BettingRepository.cs:62` | ProcessBetSettlement() |
| `../oddsfeed/OF.Settlement.Service/Repository/BettingRepository.cs:827` | SettleBet() |
| `../oddsfeed/OF.Settlement.Service/Repository/BettingRepository.cs:942` | ResettleBet() — handles re-settlement to different status |
| `../oddsfeed/OF.Shared/MessageModels/OnUOFBetSettlement.cs` | Inbound settlement message schema |
| `../oddsfeed/OF.Shared/MessageModels/OnBetSettlement.cs` | Outbound settlement result schema |

### Platform (platform-final)

| File | Role |
|------|------|
| `../platform-final/Platform/Platform.Settlement.Service/Worker.cs:101` | Consumes `r.oe.bet.settlement` etc., routes to handlers |
| `../platform-final/Platform/Platform.Settlement.Service/Repository/BettingRepository.cs` | ProcessBetSettlement(), SettleBet(), ResettleBet() |
| `../platform-final/Platform/Platform.Operator.Servicev2/Worker.cs:452` | Consumes `r.bet.operator` → ProcesssBetSettlement() |
| `../platform-final/Platform/Platform.Operator.Servicev2/Repository/BettingRepository.cs:1734` | ProcesssBetSettlement() — POST to operator API, wallet |
| `../platform-final/Platform/Platform.Sportsbook.API/Repository/SportsbookRepository.cs:8548` | Publishes `OnBetSettlement` to `r.uof.onbetsettlement` |
| `../platform-final/Platform/Platform.Shared/MessageModels/OnUOFBetSettlement.cs` | Inbound settlement message schema |
| `../platform-final/Platform/Platform.Shared/MessageModels/OnBetSettlement.cs` | Full settlement result with Bet + BetSelections |

---

## ResettleBet() Preconditions (OddsFeed)

`ResettleBet()` in `OF.Settlement.Service/Repository/BettingRepository.cs:942` **silently skips** if any of:

- New status == current status (no change)
- Current selection status is Waiting, Cancelled, Rejected, or CashedOut
- `bet.IsResettled == true` AND bet is not Running AND `isManualSettle == false`
- `bet.IsAgent == true`
- `bet.BetStatusId == CashedOut`

> **Implication for the current fix:** The activity-log write to `ActivityLog2` (June 2026) is the audit trail and feeds the Manual Market Settlement History list. The current path publishes `OnBetSettlement` to `r.oe.bet.settlement`, which calls `ResettleBet()` with `isManualSettle = false`. This means a bet where `IsResettled == true` AND not in Running status will be silently skipped. If re-settlement of already-resettled bets is required, the `OnBetBackOfficeManualSettlement` path (`isManualSettle = true`, bypasses the guard) can be used instead — but that path settles by `BetId`, not by `SelectionId`.

---

## Confirmed Findings

1. **`r.uof.onbetsettlement` had no consumer — CONFIRMED (June 2026); fix in progress**  
   Full audit of all appsettings.json files and ArgoCD staging configs across `oddsfeed`, `platform-final`, and `odds-engine-2` confirmed no service bound a queue to this routing key. Messages published by Backoffice.Web were silently discarded. `OE.Adapter.Service` is being updated to subscribe to `r.uof.#`, which will route these messages through the full OE pipeline.

2. **OE.Adapter.Service subscription is being expanded**  
   Current: subscribes only to `r.bg.receiver.#` (BetGenius feed). Planned addition: `r.uof.#` to handle Backoffice-originated manual market settlement messages. This was the original intent — the `r.uof.onbetsettlement` routing key was always designed to enter the UOF pipeline; the subscription binding was simply never configured for `odds-engine-2`.

3. **OF.Settlement.Service outbound publishing disabled**  
   The code that publishes `OnBetSettlement → r.bet.operator` after processing is commented out in `OF.Settlement.Service` ("NOT APPLICABLE IN OB"). Only `Platform.Settlement.Service` actively publishes to `r.bet.operator`.

---

## Applied Fix (June 2026)

`AddMarketSettlementModal.razor.cs` publishes `OnBetSettlement` **per fixture** to `r.uof.onbetsettlement`:

```
MessageType = "OnBetSettlement"
RoutingKey  = "r.uof.onbetsettlement"
Payload     = OnBetSettlementMessage {
    SportId, ProviderFixtureId, FixtureSpecifier,
    HomeScore: "", AwayScore: "",
    Selections: [{ MarketTemplateId, SelectionTemplateId, MarketSpecifier,
                   SelectionSpecifier, Result: bool, VoidFactor, DeadHeatFactor }]
}
```

**Source:** [Backoffice.Shared/Models/OnBetSettleMessage.cs](../Backoffice.Shared/Models/OnBetSettleMessage.cs)

This requires `OE.Adapter.Service` to subscribe to `r.uof.#` (change in progress in `odds-engine-2`). Once live, the full pipeline runs:

```
r.uof.onbetsettlement
  → OE.Adapter.Service (r.uof.# subscription)
  → r.oe.adapter.onbetsettlement.{instance}
  → OE.Sportsbook.Service (updates MongoDB, publishes per-selection)
  → r.oe.bet.settlement
  → OF.Settlement.Service + Platform.Settlement.Service
```
