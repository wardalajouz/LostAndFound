# Phase 1: Database setup

## 1. Copy the files

Copy `Backend/LostAndFound.API/Data/` and `Backend/LostAndFound.API/Program.cs` into your repo.
Nothing else changes.

## 2. Add the packages

In `Backend/LostAndFound.API`:

```
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet tool install --global dotnet-ef
```

If `dotnet add package` picks a version newer than .NET 10, add `--version 10.*`.

## 3. Get a SQL Server and add the connection string

Windows: SQL Server LocalDB comes with Visual Studio. Add this to `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "Default": "Server=(localdb)\\MSSQLLocalDB;Database=Findly;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Mac or Linux: run SQL Server in Docker:

```
docker run -d --name findly-sql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Your_Strong_Pass123' -p 127.0.0.1:1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Keep the password out of Git by storing it as a user secret:

```
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=Findly;User Id=sa;Password=Your_Strong_Pass123;TrustServerCertificate=True"
```

## 4. Create the database

```
dotnet ef migrations add InitialCreate -o Data/Migrations
dotnet ef database update
```

## 5. Check it worked

Connect with SQL Server Management Studio, Azure Data Studio, or the VS Code SQL Server extension.
You should see these tables: `Users`, `CampusLocations` (24 rows), `Posts`, `PostImages`,
`Conversations`, `Messages`, `Notifications`, `PostModerationLogs`, `UserModerationLogs`,
`SupportTickets`, `__EFMigrationsHistory`, and Identity's `AspNetRoles`, `AspNetUserRoles`,
`AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims`.

The API still returns 501 everywhere. That changes from phase 2.

## Later schema changes

Change an entity or configuration, then:

```
dotnet ef migrations add <DescriptiveName> -o Data/Migrations
dotnet ef database update
```

Commit the `Data/Migrations` folder. Never edit a migration that teammates have already applied.
