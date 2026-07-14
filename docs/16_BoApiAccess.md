# BO API Access (Local / Ad-hoc Debugging)

**Purpose:** How to authenticate to the deployed **Backoffice API** to inspect behaviour during debugging (e.g. replay an endpoint exactly as `Backoffice.Web` does). Read-only investigation; obey the safety rules in `CLAUDE.md`.

> **Environments:** Use **Staging** only unless explicitly authorised for Production in the current conversation. Production access is never assumed.

---

## Credentials location

Real credentials are **not** stored in git. They live in a git-ignored file at the repo root:

```
.env.bo-api.local        # git-ignored (.gitignore: .env.*.local)
```

If it is missing, create it from this template (Staging values):

```dotenv
BO_API_BASE=https://bo-api.stg.song88.com
BO_WEB_BASE=https://bo.stg.song88.com
BO_API_KEY=<Security:ApiKey from Backoffice.API/appsettings.json>
BO_USERNAME=<bo username>
BO_PASSWORD=<bo password>
```

- `BO_API_KEY` is the `Security:ApiKey` value (root of `Backoffice.API/appsettings.json`; also in `argocd/apps/corp-apps/bo-api/stg.yaml` as `Security__ApiKey`).
- Username/password are normal BO login credentials — the BO API proxies `login` to the internal Auth API.

---

## Auth model (why two headers are needed)

Most BO API controllers require **both**:

1. **`x-api-key`** header — validated by `ApiKeyFilter` against `Security:ApiKey`
   (`Backoffice.API/ActionFilter/ApiKeyFilter.cs`). Applied via `ServiceFilter(typeof(ApiKeyFilter))` on the controller.
2. **`Authorization: Bearer <JWT>`** — validated by the custom `AuthorizeAttribute`
   (`Backoffice.API/ActionFilter/AuthorizeAttribute.cs`): checks issuer/expiry, re-validates the token against the Auth API, and (if the attribute names a claim) requires that permission claim at the right `Access` level.

`Access` enum (`Backoffice.Shared/Enums/Access.cs`): `NoAccess=0, Edit=1, View=2`.
Note the `AuthorizeAttribute` logic: a `View` requirement only blocks `NoAccess`; an `Edit` requirement blocks `View`-only holders.

The login endpoint itself (`POST api/authentication/login`) is **not** claim-gated.

---

## Step 1 — Get a JWT

```bash
# from repo root
set -a; . ./.env.bo-api.local; set +a

TOKEN=$(curl -s --max-time 20 -X POST "$BO_API_BASE/api/authentication/login" \
  -H "Content-Type: application/json" \
  -d "{\"Username\":\"$BO_USERNAME\",\"Password\":\"$BO_PASSWORD\"}" \
  | grep -oE '"accessToken":"[^"]+"' | sed 's/"accessToken":"//;s/"$//')

echo "token length: ${#TOKEN}"
```

A successful login returns `{"statusCode":100,"message":"Success","result":{"accessToken":"...","operatorToken":"..."}}`.
To see the permission claims inside the token, base64-decode the middle JWT segment.

## Step 2 — Call an authenticated endpoint

```bash
# Example: Market Settlement history (TradingController, gated Admin.Settlements.MarketSettlement View)
curl -s --max-time 25 -X POST "$BO_API_BASE/api/trading/manual/marketsettlement/history" \
  -H "x-api-key: $BO_API_KEY" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"Page":"Admin.Settlements.MarketSettlement","Payload":"%",
       "DateFrom":"2026-06-24T00:00:00","DateTo":"2026-06-26T23:59:59",
       "MetaData":{"CurrentPage":1,"PageSize":10}}'
```

### Reading the response
- `{"title":"Unauthorized","message":"..."}` → the **JWT** failed the `AuthorizeAttribute` (missing/blocked claim, or expired/invalid token). The body `message` echoes the auth result message.
- `{"Title":"Unauthorized","StatusCode":401,"Message":"Missing Or Unsupported APIKey..."}` → the **`x-api-key`** is missing/wrong.
- `{"statusCode":...,"result":{...}}` → success; inspect `result`.

---

## Notes
- Endpoints under `ConfigController` (`api/config/...`) require the `SBC.OE.Config` claim at class level — a frequent source of "401 → empty list" symptoms when a feature with a *different* permission borrows one of those endpoints.
- The deployed staging image tag is the moving `latest` (`argocd/apps/corp-apps/bo-api/stg.yaml`); a route only exists once that build is rolled.
- Keep this strictly to lower environments. Never add Production credentials to `.env.bo-api.local`.
