# Phase 2: Auth

Register, login, forgot/reset password, JWT on protected endpoints, the Admin role, and a seeded admin.
Every other endpoint still returns 501.

## 1. Copy the files

Unzip into the repo root. It adds or replaces:

- `Backend/LostAndFound.API/` : `Auth/`, `Email/`, `OpenApi/`, `ErrorHandling/`, `Services/AuthService.cs`,
  every controller (authorization attributes), `Program.cs`, `appsettings.json`, `.gitignore`
- `Backend/LostAndFound.API.Tests/` : the new test project

Your `.csproj`, `appsettings.Development.json`, and `launchSettings.json` are not replaced.

## 2. Add the packages

```
cd Backend/LostAndFound.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.*
dotnet add package MailKit
cd ..
dotnet sln LostAndFound.API/LostAndFound.API.slnx add LostAndFound.API.Tests/LostAndFound.API.Tests.csproj
```

## 3. Set the secrets (each developer, once)

From `Backend/LostAndFound.API`. If you never ran `dotnet user-secrets init`, run it first.

```
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>"
dotnet user-secrets set "AdminSeed:Email" "security@findly.local"
dotnet user-secrets set "AdminSeed:Password" "<a strong password>"
```

Generate a key with `openssl rand -base64 48` (Mac/Linux) or
`[Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }) -as [byte[]])` (PowerShell).

The API refuses to start if the signing key is missing or shorter than 32 characters.
If `AdminSeed` is not set, no admin account is created.

## 4. Run the tests

```
cd Backend
dotnet test LostAndFound.API.Tests
```

Expected: `Passed! - Failed: 0, Passed: 106`. The tests use in-memory SQLite, so they need no SQL Server.

## 5. Try it

`dotnet run`, open `http://localhost:5176/swagger`:

1. `POST /api/auth/register`, then copy `accessToken` from the response.
2. Click **Authorize**, paste the token.
3. `GET /api/users/me` now returns 501 instead of 401.
4. `POST /api/auth/forgot-password` with your email. In Development the email is written to
   `Backend/LostAndFound.API/App_Data/emails/` (ignored by Git). Open it to see the reset link.

No database migration is needed. Phase 2 does not change any table.

## Sending real emails

Outside Development the API will not start without SMTP settings, so reset emails are never silently dropped:

```
dotnet user-secrets set "Email:Smtp:Host" "smtp-relay.brevo.com"
dotnet user-secrets set "Email:Smtp:Port" "587"
dotnet user-secrets set "Email:Smtp:Username" "<username>"
dotnet user-secrets set "Email:Smtp:Password" "<password>"
```

Set `Frontend:ResetPasswordUrl` to the frontend's reset page. The link it receives is
`<ResetPasswordUrl>?email=...&token=...`.

## For the frontend

- Send the token on every protected request: `Authorization: Bearer <accessToken>`.
- Tokens last 60 minutes (`expiresAt`). On a 401, send the user to the login page.
- All errors use the same JSON shape: `title`, `status`, `detail`, and `errors` for field problems.

| Endpoint | Result |
|---|---|
| Register | 201 token; 400 field errors; 409 email already used |
| Login | 200 token; 401 wrong email/password or locked (5 failures = 15 minutes); 403 account disabled |
| Forgot password | Always 202 |
| Reset password | 204; 400 `errors.Token` (bad or used link) or `errors.NewPassword` |

Password rule: at least 8 characters, with an uppercase letter, a lowercase letter, and a digit.
