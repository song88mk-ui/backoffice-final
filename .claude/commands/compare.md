# /compare

Compare two implementations, approaches, files, or branches in the Backoffice solution and recommend the better option.

## Usage

```
/compare $ARGUMENTS
```

`$ARGUMENTS` examples:
- `BackofficeRepository.GetClientsAsync vs ClientController.GetClients` — two code paths
- `in-process IMemCache vs MongoDB cache` — architectural approaches
- `System.Text.Json vs Newtonsoft.Json` — library choices
- `branch:feature/new-config vs main` — branch diff
- `IClientRepository v1 vs v2` — two versions of an interface

## Comparison report format

### Subject
State in one sentence what is being compared and why it matters.

### Side-by-side Summary

| Dimension | Option A | Option B |
|-----------|----------|----------|
| Correctness | | |
| Performance | | |
| CLAUDE.md compliance | | |
| Maintainability | | |
| Backward compatibility | | |
| Security | | |

Rate each cell: ✓ Good / ~ Acceptable / ✗ Concern.

### Detailed Analysis

For each dimension where the options differ, explain:
- What Option A does and why it scores as it does
- What Option B does and why it scores as it does
- Cite file paths and line numbers where the behavior is visible

### CLAUDE.md Alignment

For each option, list which CLAUDE.md rules it follows and which it violates:

```
Option A
  ✓ Returns BaseResponse<T>
  ✓ AsNoTracking() on read queries
  ✗ Missing try/catch — violates error handling convention
  ✗ Creates HttpClient with new — known performance anti-pattern

Option B
  ✓ Returns BaseResponse<T>
  ✓ AsNoTracking() on read queries
  ✓ Full try/catch with structured logging
  ~ Uses Newtonsoft.Json (legacy, but CLAUDE.md permits for existing code)
```

### Recommendation

State clearly which option to use and why in 2–4 sentences. If neither is ideal, describe the hybrid approach. Do not hedge — pick one.

### Migration Path (if applicable)

If recommending Option B but Option A is already in production, list the steps to migrate safely, respecting backward compatibility rules from CLAUDE.md.
