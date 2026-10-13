# AGENTS.md — backoffice-final

The song88 back-office: Blazor Server UI, REST API, SignalR hub and three RabbitMQ workers sharing
`Backoffice.Shared`. Read [CLAUDE.md](CLAUDE.md) first: it holds the standing rules every agent
follows and the full operating manual. This file is the short version for agents.

## Mission boundaries
- Subject = backoffice-final (the admin UI and its services; it reads the sportsbook MongoDB but
  never writes it).
- Only modify files inside this repo; siblings (`Auth`, `odds-engine-2`, `platform-final`,
  `platform-web`, `bet-genius`, `webprops-api`, `argocd`, `infrastructure`) are read-only
  references.
- Schema lives in `../infrastructure/flyway`, not here: this repo has no EF Core migrations.

## Hard rules
- Before touching any environment, verify the connection string, hostname and env var; "looks like
  dev" is not confirmation (CLAUDE.md § Critical rules, rule 1).
- Publishing `OnDeleteCacheMessage` on `r.oe.cachesync.*`, or anything that cascades to the
  OddsEngine, needs confirmation (CLAUDE.md § Critical rules, rule 2).
- Confirm `RabbitMQ:Host` is non-production before running anything that publishes (CLAUDE.md §
  Critical rules, rule 3).
- No SSMS, Azure Data Studio or Compass writes to Production; data changes go through the API
  (CLAUDE.md § Critical rules, rule 4).
- Flyway scripts: confirm the target host, and no column drop or type change without a reviewed
  rollback plan (CLAUDE.md § Critical rules, rule 5).
- A `Backoffice.Shared` change hits all 6 other projects; document deploy order and rollback first
  (CLAUDE.md § Critical rules, rule 6).
- Never quote a `FromSql` interpolation: `FromSql($"Proc {x}")`, not `'{x}'` (CLAUDE.md § Critical
  rules, rule 8).
- Commit messages are `[{ticket}] {Message}`, one domain per commit (CLAUDE.md § How this team
  writes code).

## See also
[CLAUDE.md](CLAUDE.md) · [.claude/README.md](.claude/README.md) (agent catalogue) ·
[docs/00_INDEX.md](docs/00_INDEX.md)
