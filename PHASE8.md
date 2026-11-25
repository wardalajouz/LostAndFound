# Phase 8: Admin panel

The Security Admin dashboard. No new packages, no settings, no migration.

All endpoints need an admin login (`AdminSeed` in user secrets). Normal users get 403.

## Endpoints now working

| Endpoint | Result |
|---|---|
| `GET /api/admin/stats` | Totals, per category, per campus location, and the last 12 months |
| `GET /api/admin/posts` | Every post, including hidden and deleted. Filters: `search`, `type`, `category`, `status`, `campusLocationId`, `handedToSecurity`, `visibility`, `sort`, paging |
| `GET /api/admin/posts/{id}` | The post, its verification details, visibility, and moderation history (newest first) |
| `PUT /api/admin/posts/{id}/visibility` with `{ "visibility": "Hidden", "reason": "..." }` | 204. `Visible`, `Hidden`, or `Deleted` |
| `GET /api/admin/users` | Accounts that are not deleted. Filters: `search` (name or email), `userType`, `status`, paging |
| `PUT /api/admin/users/{id}/status` with `{ "status": "Disabled", "reason": "..." }` | 204. `Active` or `Disabled` |

**Resolved Cases** page: `GET /api/admin/posts?status=Resolved`.
**Security Handover** page: `GET /api/admin/posts?handedToSecurity=true`.

## Rules

- Statistics do not count deleted posts. Hidden posts are counted.
  `byCategory` always lists all 8 categories; `byMonth` always has 12 months, oldest first, in Jordan time;
  `byCampusLocation` lists locations that have posts, busiest first.
- Every visibility or status change needs a reason of at least 3 characters (spaces alone do not count).
  It is saved with the admin who made it. The post owner is notified of visibility changes.
- Setting the visibility or status an item already has is a 409, so the history has no duplicates.
- Posts of a deleted account cannot be shown again (409).
- Disabling an account signs that user out immediately, everywhere. Reactivating lets them back in.
- Admins cannot disable or reactivate admin accounts, including their own (409), so no admin can lock out the others.

## Also in this phase

- No endpoint returns "501 Not Implemented" anymore, so the temporary 501 handler was removed.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 433`.
Run the SQL Server check from `CHECK-ON-SQLSERVER.md` too.
