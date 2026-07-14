# Translation System

Repository-specific knowledge for the Backoffice multi-language translation system. Apply this when adding new translatable entities, fixing missing translations, or tracing language fallback bugs.

---

## Architecture overview

Translations are stored in a **dedicated database** accessed via `TranslationContext` (defined in `Backoffice.Shared/Data/`). This context is completely separate from entity data — translations are never embedded in the entity tables.

```
Entity (BackofficeContext / SportsbookContext)
    + LanguageCode (from request)
        ↓
TranslationContext lookup
        ↓
Four-level fallback → always returns a string, never null
```

## TranslationContext entities

| Entity | Fields | Foreign key |
|--------|--------|-------------|
| `FixtureTranslation` | `FixtureId (long)`, `LanguageCode`, `Name` | `FixtureId` |
| `MarketTranslation` | `MarketId (long)`, `LanguageCode`, `Name` | `MarketId` |
| `LeagueTranslation` | `LeagueId (int)`, `LanguageCode`, `Name` | `LeagueId` |
| `SportTranslation` | `SportId (int)`, `LanguageCode`, `Name` | `SportId` |
| `SelectionTranslation` | `SelectionId (long)`, `LanguageCode`, `Name` | `SelectionId` |
| `SegmentTranslation` | `SegmentId (int)`, `LanguageCode`, `Name` | `SegmentId` |

## Language code format

- Short ISO 639-1: `en`, `es`, `fr`, `pt`, `zh`, `ko`, `ja`
- Region variants: `en-US`, `pt-BR`, `zh-HK`, `zh-TW`

Stored as `varchar(10)` — always lowercase. Never store locale in uppercase.

## Four-level fallback chain — must never return null

```
1. Exact: languageCode = "pt-BR"
2. Base:  languageCode = "pt"        (strip the region suffix)
3. Default: languageCode = "en"
4. Raw name from the entity itself   (e.g. League.Name)
```

The production implementation must follow all four levels. Any shortcut that skips a level will surface as blank names in the UI for some users.

```csharp
private async Task<string> ResolveTranslationAsync(
    int entityId, string languageCode, string fallbackName,
    Func<int, string, Task<string>> fetchTranslation)
{
    // 1. Exact match
    var name = await fetchTranslation(entityId, languageCode);
    if (name != null) return name;

    // 2. Language base (strip region)
    if (languageCode.Contains('-'))
    {
        var baseLang = languageCode.Split('-')[0];
        name = await fetchTranslation(entityId, baseLang);
        if (name != null) return name;
    }

    // 3. Default language
    name = await fetchTranslation(entityId, "en");
    if (name != null) return name;

    // 4. Raw entity name
    return fallbackName;
}
```

## Inserting a new translatable entity

When creating a new entity that has a `Name`, always insert the English translation in the same `TransactionScope`:

```csharp
using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
    // Insert entity
    var league = new League { Name = request.Name, SportId = request.SportId };
    _sportsbookContext.Leagues.Add(league);
    await _sportsbookContext.SaveChangesAsync();

    // Insert default English translation (required for fallback chain)
    _translationContext.LeagueTranslations.Add(new LeagueTranslation
    {
        LeagueId = league.LeagueId,
        LanguageCode = "en",
        Name = request.Name
    });

    // Insert additional translations if provided
    foreach (var t in request.Translations ?? Enumerable.Empty<TranslationInput>())
    {
        _translationContext.LeagueTranslations.Add(new LeagueTranslation
        {
            LeagueId = league.LeagueId,
            LanguageCode = t.LanguageCode,
            Name = t.Name
        });
    }

    await _translationContext.SaveChangesAsync();
    transaction.Complete();
}
```

**Rule**: Always insert at minimum the `en` translation. Without it, the third fallback level fails and the entity gets its raw name — which may be an internal code.

## Updating a translation

Translations use upsert semantics — insert if missing, update if present:

```csharp
var existing = await _translationContext.LeagueTranslations
    .FirstOrDefaultAsync(t => t.LeagueId == id && t.LanguageCode == lang);

if (existing != null)
    existing.Name = newName;
else
    _translationContext.LeagueTranslations.Add(new LeagueTranslation
    {
        LeagueId = id, LanguageCode = lang, Name = newName
    });

await _translationContext.SaveChangesAsync();
```

## Reading with translation in a repository response

Always accept `languageCode` as a method parameter — never default it silently:

```csharp
public async Task<BaseResponse<LeagueModel>> GetLeagueAsync(int leagueId, string languageCode)
{
    try
    {
        var league = await _sportsbookContext.Leagues
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LeagueId == leagueId);

        if (league == null)
            return new BaseResponse<LeagueModel> { StatusCode = StatusCode.NotFound };

        var translatedName = await ResolveTranslationAsync(
            leagueId, languageCode, league.Name,
            async (id, lang) => await _translationContext.LeagueTranslations
                .AsNoTracking()
                .Where(t => t.LeagueId == id && t.LanguageCode == lang)
                .Select(t => t.Name)
                .FirstOrDefaultAsync());

        return new BaseResponse<LeagueModel>
        {
            Result = new LeagueModel
            {
                LeagueId = leagueId,
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

## Caching translated results

Translated results can be cached — include the language code in the cache key:

```
league.{leagueId}.{languageCode}
fixture.{fixtureId}.{languageCode}
```

Invalidate when the entity **or** any of its translations changes.

## Background services and TransactionScope

Background services (`Config.Service`, `Utility`, `WebSocket.Service`) must **not** use `TransactionScope`. If a background service needs to write translations, use separate `SaveChangesAsync` calls — accept that they are not atomic.
