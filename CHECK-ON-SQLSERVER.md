# Check every feature on real SQL Server

Runs all 336 tests against SQL Server LocalDB on your laptop. Each test API instance gets its own
new database, created from the real migration, and dropped afterwards. Your `Findly` database is not touched.

## Run it

PowerShell, from the repo's `Backend` folder:

```powershell
$env:FINDLY_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test LostAndFound.API.Tests
```

Expected: `Passed! - Failed: 0, Passed: 336`.

It takes 1 to 3 minutes instead of a few seconds, because it creates about 40 small databases.
If it finishes in a few seconds, the variable was not set and the tests ran on SQLite instead.

To go back to the fast SQLite run, close PowerShell or run `Remove-Item Env:FINDLY_TEST_SQLSERVER`.

## If something fails

Copy the full output of the failing test (its name, `Expected`, `Actual`, and the stack trace) and send it.

If LocalDB is not found, check its name with `sqllocaldb info` and change `MSSQLLocalDB` to match.

## Then check by hand in Swagger

`dotnet run`, open `http://localhost:5176/swagger`. These are the parts no automated test covers:

1. Register, copy `accessToken`, click **Authorize** and paste it.
2. `POST /api/posts/{id}/images` with a real phone photo. Open the returned URL in the browser:
   it should be upright and load as an image.
3. `POST /api/auth/forgot-password` with your email, then open the file in `App_Data/emails/`
   and use the link's `email` and `token` in `POST /api/auth/reset-password`.
