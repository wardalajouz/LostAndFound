# Phase 3: Posts

Create, edit, delete, and resolve posts; browse with search, filters, sort, and paging;
My Items; dashboard stats; and the campus location list.

No new packages, no new settings, and no database migration: the tables are unchanged.

## Endpoints now working

| Endpoint | Who | Result |
|---|---|---|
| `GET /api/campus-locations` | Anyone | Active locations, in the request's language |
| `GET /api/posts` | Anyone | Active, visible posts. `search`, `type`, `category`, `campusLocationId`, `date`, `sort`, `page`, `pageSize` |
| `GET /api/posts/{id}` | Anyone | Visible post (active or resolved), without verification details |
| `POST /api/posts` | Signed in | 201 with the post |
| `PUT /api/posts/{id}` | Owner | 200; 409 if resolved |
| `DELETE /api/posts/{id}` | Owner | 204 (hidden, not removed) |
| `POST /api/posts/{id}/resolve` | Owner | 204 (doing it twice is harmless) |
| `GET /api/users/me/posts` | Signed in | My posts, including resolved and admin-hidden. `search`, `type`, `status`, `sort`, paging |
| `GET /api/users/me/posts/{id}` | Owner | The post plus `verificationDetails`, for the edit form |
| `GET /api/users/me/posts/stats` | Signed in | `lostCount`, `foundCount`, `activeCount`, `resolvedCount` |

## Rules

- Someone else's post, a deleted post, or a missing post all return **404**, so nobody can find out which posts exist.
- The owner always comes from the login token, never from the request body.
- Verification details are only returned by `GET /api/users/me/posts/{id}`.
- Lost posts cannot have "handed to security" or verification details.
- Resolved posts cannot be edited, but can be deleted.
- Search matches item name, description, brand, and color, ignoring upper/lower case.
  `%`, `_`, and `[` are searched as normal characters.
- The location must be from the active list. A post keeps its location if that location is later retired.
- Images are empty for now. Uploading arrives in phase 4.

## Language

Campus location names follow the `Accept-Language` header: `en` gives English, anything else
(or no header) gives Arabic. The frontend's language toggle should send this header.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 162`.
They include tests that run every new query through the SQL Server provider, so queries
SQL Server cannot handle are caught without needing a database.
