# Phase 9: Quality and security

No new packages and no migration. New settings in `appsettings.json`: `Cors` and `RateLimiting`.

## CORS (lets the frontend call the API from a browser)

```json
"Cors": {
  "AllowedOrigins": [ "http://localhost:5173" ]
}
```

- List every frontend address exactly: scheme and host (and port if not the default), no path, no trailing slash.
  For example `"https://findly.example"`. Wildcards are refused.
- The API refuses to start if an entry is invalid.
- Allowed: GET, POST, PUT, DELETE and the `Authorization`, `Content-Type`, `Accept-Language` headers.
  The frontend can read the `Location` and `Retry-After` response headers.
- Cookies are not used (the token goes in the `Authorization` header), so credentials stay off.
- Add the live frontend's address here before deployment (phase 10).

## Rate limits

```json
"RateLimiting": {
  "Auth":     { "PermitLimit": 10, "WindowSeconds": 60 },
  "Support":  { "PermitLimit": 5,  "WindowSeconds": 600 },
  "Messages": { "PermitLimit": 30, "WindowSeconds": 60 }
}
```

| Limit | Applies to | Counted per |
|---|---|---|
| Auth | register, login, forgot and reset password (together) | client IP |
| Support | support tickets | client IP |
| Messages | sending chat messages | signed-in user |

Over the limit: **429** with a `Retry-After` header (seconds) and the usual error JSON.
Requests blocked by the limit never reach the password check, so they do not count toward account lockout.

## Other protections

- Every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`,
  and a content security policy that allows nothing to run (Swagger is left out of the policy so it keeps working).
  Error responses get them too.
- Outside Development, browsers are told to use HTTPS only (HSTS).
- JSON request bodies over 1 MB are refused with **413**. Image uploads keep their own, larger limits.
- In Production, server errors return a generic message with no exception details, and Swagger is off.

## Security log

Logged with account ids, never with emails, names, passwords, or tokens:
failed logins, lockouts, disabled-account logins, password reset requested and completed, password and email changes,
account deletion, admin visibility and status changes, and rate limit rejections.
All logging uses compiled log messages (faster, and checked at build time).

## Review results

- No known vulnerable packages, including indirect ones.
- Code analysis at the "recommended" level: all warnings fixed except three that are safe to leave:
  - in the generated migration file (generated code, not edited by hand);
  - `Substring` in a database query, which must stay because `Span` cannot be translated to SQL;
  - the project is named `LostAndFound.API` while the code uses `LostAndFound.Api`, which only matters for tools that compare names by letter case.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 478`.
Some tests run the API on a real Kestrel server (bound to 127.0.0.1 only), because size limits are enforced there.
