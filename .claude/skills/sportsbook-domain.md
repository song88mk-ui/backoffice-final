# Sportsbook Domain

Domain knowledge for the sportsbook data model — fixtures, markets, selections, leagues, and sports. Apply this when working with `SportsbookContext`, MongoDB Sportsbook collections, or trading data.

---

## Entity hierarchy

```
Sport
└── League
    └── Fixture  (an event, e.g. "Man Utd vs Liverpool")
        └── Market  (a bet type, e.g. "Match Result")
            └── Selection  (an outcome, e.g. "Home Win")
```

Segments are a parallel classification layer used for grouping and routing:
```
Segment (hierarchical, can be nested)
└── assigned to Fixtures/Leagues/Sports
```

## Data residency

| Entity | SQL (source of truth) | MongoDB (cache) |
|--------|-----------------------|----------------|
| Sport | `TradingContext` / `SportsbookContext` | `Sports` collection |
| League | `TradingContext` / `SportsbookContext` | `Leagues` collection |
| Fixture | `SportsbookContext` | `Fixtures` collection |
| Market | `SportsbookContext` | `Markets` collection |
| Selection | `SportsbookContext` | `Selections` collection |
| Segment | `BackofficeContext` | `Segments` collection |

Translations live exclusively in `TranslationContext` — never in MongoDB or the trading/sportsbook SQL tables.

## Fixture lifecycle states

Fixtures move through states during their lifecycle. Always check the fixture status field before assuming a fixture is tradeable:

```
Prematch → Live → Resulted → Settled
                ↓
             Suspended (temporary, returns to Live)
             Cancelled (terminal)
             Postponed (terminal for that date)
```

When a fixture is `Cancelled` or `Settled`, its markets and selections are typically read-only. Do not allow configuration writes against settled fixtures.

## Market types

Markets are typed — the type determines how selections are structured and how odds are displayed:
- **Match Result** — 3 selections (Home / Draw / Away)
- **Over/Under** — 2 selections (Over / Under a line)
- **Asian Handicap** — 2 selections with a handicap value
- **Correct Score** — many selections
- **Both Teams to Score** — 2 selections (Yes / No)

Market type affects validation in bet configuration. When writing config rules, check the market type to determine valid selection counts.

## Odds representation

Odds are stored as `long` values representing **decimal odds × 1000** (e.g. `1500` = odds of `1.5`). This avoids floating-point precision issues.

When displaying:
```csharp
decimal displayOdds = storedLong / 1000m;  // 1500 → 1.5
```

When storing:
```csharp
long storeOdds = (long)(decimalOdds * 1000);  // 1.5 → 1500
```

## Client margin model

Client margins are applied on top of base odds. Margin is stored as a `long` in basis points (e.g. `200` = 2%):

```
Effective odds = base odds × (1 - margin / 10000)
```

Margins can be configured at multiple levels:
1. Client-level (applies to all markets)
2. League-level (overrides client-level for a specific league)
3. Market-level (overrides league-level for a specific market)

Most specific level wins. `BackofficeRepository` contains the hierarchy resolution logic — do not duplicate it.

## Segment model

Segments are hierarchical groupings used for odds engine routing and display filtering:

```csharp
public class Segment
{
    public int SegmentId { get; set; }
    public int? ParentSegmentId { get; set; }  // null = root
    public string Name { get; set; }
    public string Code { get; set; }
}
```

When a client is assigned to a segment, they see only fixtures/leagues within that segment tree. The `BackofficeRepository` resolves segment ancestry when building client views.

## Config template model

Config templates are reusable bet setting presets that can be applied to multiple clients:

```
ConfigTemplate
└── ConfigTemplateMargin (one per market type or league)
```

When a template is applied to a client:
1. The template's margins are copied to `ClientBetSetting`
2. A RabbitMQ message `OnUpdateClientMarketConfigMessage` is published
3. `Backoffice.Config.Service` processes the message and refreshes its local state

## Key identifiers

| Entity | Identifier type | Notes |
|--------|----------------|-------|
| Sport | `int SportId` | Stable, never reassigned |
| League | `int LeagueId` | Stable |
| Fixture | `long FixtureId` | Long because high-volume creation |
| Market | `long MarketId` | Long |
| Selection | `long SelectionId` | Long |
| Client | `int ClientId` | Backoffice internal |
| Segment | `int SegmentId` | |

Never confuse `int` and `long` identifiers — casting errors between trading and sportsbook contexts are a common source of data loss bugs.
