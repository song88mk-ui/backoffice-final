# /translation

Add, fix, or trace translation logic in the Backoffice solution using `TranslationContext` and the multi-language fallback chain.

## Usage

```
/translation $ARGUMENTS
```

`$ARGUMENTS` examples:
- `add translation support to SegmentRepository.GetSegmentAsync`
- `trace translation fallback for fixtureId 500 in language pt-BR`
- `add LeagueTranslation for a new league insert`
- `review TranslationContext entities`
- `fix missing fallback in MarketTranslation lookup`

## Translation architecture

Translation data lives in the **TranslationContext** (Translation database), separate from all other data.

### Entities

| Entity | Table (assumed) | Key fields |
|--------|----------------|------------|
| `FixtureTranslation` | FixtureTranslations | `FixtureId`, `LanguageCode`, `Name` |
| `MarketTranslation` | MarketTranslations | `MarketId`, `LanguageCode`, `Name` |
| `LeagueTranslation` | LeagueTranslations | `LeagueId`, `LanguageCode`, `Name` |
| `SportTranslation` | SportTranslations | `SportId`, `LanguageCode`, `Name` |
| `SelectionTranslation` | SelectionTranslations | `SelectionId`, `LanguageCode`, `Name` |
| `SegmentTranslation` | SegmentTranslations | `SegmentId`, `LanguageCode`, `Name` |

### Language code format
- ISO 639-1: `en`, `es`, `fr`, `pt`, `zh`
- Region variants: `en-US`, `pt-BR`, `zh-HK`

## Fallback chain (required — never return null)

When a translation is missing for the requested language, apply this chain in order:

1. **Exact match** — `languageCode == "pt-BR"`
2. **Language base** — `languageCode == "pt"` (strip region suffix)
3. **Default language** — `languageCode == "en"`
4. **Entity name/code** — the raw name stored on the entity itself

```csharp
private async Task<string> GetTranslationAsync(
    int entityId, string languageCode, string defaultName)
{
    // 1. Exact match
    var exact = await _translationContext.FixtureTranslations
        .AsNoTracking()
        .Where(t => t.FixtureId == entityId && t.LanguageCode == languageCode)
        .Select(t => t.Name)
        .FirstOrDefaultAsync();
    if (exact != null) return exact;

    // 2. Language base
    var baseLang = languageCode.Contains('-')
        ? languageCode.Split('-')[0]
        : null;
    if (baseLang != null)
    {
        var baseMatch = await _translationContext.FixtureTranslations
            .AsNoTracking()
            .Where(t => t.FixtureId == entityId && t.LanguageCode == baseLang)
            .Select(t => t.Name)
            .FirstOrDefaultAsync();
        if (baseMatch != null) return baseMatch;
    }

    // 3. Default language
    var defaultTranslation = await _translationContext.FixtureTranslations
        .AsNoTracking()
        .Where(t => t.FixtureId == entityId && t.LanguageCode == "en")
        .Select(t => t.Name)
        .FirstOrDefaultAsync();
    if (defaultTranslation != null) return defaultTranslation;

    // 4. Raw entity name
    return defaultName;
}
```

## Adding translations when inserting entities

When a new entity is created, insert its default translation in the same `TransactionScope`:

```csharp
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    // Insert entity
    _backofficeContext.Leagues.Add(league);
    await _backofficeContext.SaveChangesAsync();

    // Insert default translation
    _translationContext.LeagueTranslations.Add(new LeagueTranslation
    {
        LeagueId = league.Id,
        LanguageCode = "en",
        Name = league.Name
    });
    await _translationContext.SaveChangesAsync();

    transaction.Complete();
}
```

## Reading translations in repository methods

Pattern for returning entity with translation:

```csharp
public async Task<BaseResponse<LeagueModel>> GetLeagueAsync(int leagueId, string languageCode)
{
    try
    {
        var league = await _backofficeContext.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == leagueId);

        if (league == null)
            return new BaseResponse<LeagueModel> { StatusCode = StatusCode.NotFound };

        var translatedName = await GetTranslationAsync(leagueId, languageCode, league.Name);

        return new BaseResponse<LeagueModel>
        {
            Result = new LeagueModel
            {
                Id = league.Id,
                Name = translatedName,
                LanguageCode = languageCode
            }
        };
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to get League {LeagueId}: {Exception}", leagueId, ex);
        return new BaseResponse<LeagueModel> { StatusCode = StatusCode.Error };
    }
}
```

## Key rules

1. **Never return null for a translation.** Always apply the four-level fallback.
2. **Use `AsNoTracking()`** on all `TranslationContext` reads.
3. **Use `TransactionScope`** when writing to both `BackofficeContext` and `TranslationContext` in the same operation.
4. **TranslationContext is read-heavy** — cache translated results with `IMemCache` where the entity data is stable.
5. **Background services must not use `TransactionScope`.**

## Output format

For add-translation tasks: show the entity insert + translation insert wrapped in `TransactionScope`, plus the fallback retrieval method.
For trace tasks: follow the code path from request through fallback levels and identify where the chain breaks.
For fix tasks: show before/after diff with the broken and corrected fallback logic.
For review tasks: list all translation entities, note which have fallback logic and which don't.
