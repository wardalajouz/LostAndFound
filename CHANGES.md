# Findly API contract

Every endpoint validates its input, then returns `501 Not Implemented` with
`"detail": "This feature is not implemented yet."` until its service is implemented.
Run the API and open `/swagger` to see every request and response shape.

## How to apply

1. Copy the `Backend` folder over the `Backend` folder in your repo, replacing files.
2. Delete `Backend/LostAndFound.API/Contracts/Posts/LocationDto.cs`. It is no longer used.
3. Run `dotnet build` in `Backend/LostAndFound.API`.

Not touched (keep your copies): `LostAndFound.API.csproj`, `LostAndFound.API.slnx`,
`appsettings*.json`, `Properties/launchSettings.json`.

## Endpoints

| Area | Method and route | Auth |
|---|---|---|
| Auth | `POST /api/auth/register` | Guest |
| | `POST /api/auth/login` | Guest |
| | `POST /api/auth/forgot-password` (always 202) | Guest |
| | `POST /api/auth/reset-password` (email + token from the link) | Guest |
| Lookups | `GET /api/campus-locations` | Guest |
| Posts | `GET /api/posts` (search, filters, sort, paging) | Guest |
| | `GET /api/posts/{id}` | Guest |
| | `POST /api/posts` | User |
| | `PUT /api/posts/{id}` | Owner |
| | `DELETE /api/posts/{id}` | Owner |
| | `POST /api/posts/{id}/resolve` | Owner |
| Images | `POST /api/posts/{postId}/images` (multipart, 1 to 5 files) | Owner |
| | `DELETE /api/posts/{postId}/images/{imageId}` | Owner |
| My items | `GET /api/users/me/posts` | User |
| | `GET /api/users/me/posts/{id}` (includes verification details) | Owner |
| | `GET /api/users/me/posts/stats` | User |
| Profile | `GET /api/users/me` | User |
| | `PUT /api/users/me` | User |
| | `PUT /api/users/me/avatar` (multipart) | User |
| | `PUT /api/users/me/password` | User |
| | `GET /api/users/me/settings` | User |
| | `PUT /api/users/me/settings` | User |
| | `DELETE /api/users/me` | User |
| Chat | `POST /api/conversations` ("Message Owner", returns existing or new) | User |
| | `GET /api/conversations` | User |
| | `GET /api/conversations/{id}` | Participant |
| | `GET /api/conversations/{id}/messages` | Participant |
| | `POST /api/conversations/{id}/messages` | Participant |
| | `POST /api/conversations/{id}/read` | Participant |
| Notifications | `GET /api/notifications?unreadOnly=` | User |
| | `GET /api/notifications/unread-count` | User |
| | `POST /api/notifications/{id}/read` | User |
| | `POST /api/notifications/read-all` | User |
| Support | `POST /api/support-tickets` | Guest |
| Admin | `GET /api/admin/stats` | Admin |
| | `GET /api/admin/posts` (also Resolved Cases and Security Handover via filters) | Admin |
| | `GET /api/admin/posts/{id}` (includes verification details and moderation history) | Admin |
| | `PUT /api/admin/posts/{id}/visibility` (Visible, Hidden, Deleted + reason) | Admin |
| | `GET /api/admin/users` | Admin |
| | `PUT /api/admin/users/{id}/status` (Active, Disabled + reason) | Admin |

The Auth column is the intended rule. Nothing is enforced yet, because login is not
implemented. Add `[Authorize]` when JWT authentication is set up.

## Decisions

- Enums are sent and returned as strings, for example `"Found"`, not `1`.
- Lists are paged: `?page=1&pageSize=20` (max 50). Responses have `items`, `page`, `pageSize`, `totalCount`.
- Images are uploaded after the post is created, as a separate multipart request.
- Admin delete is soft, so a deleted post can be restored.
- Not included yet: live chat (SignalR), logout endpoint
  (with JWT the frontend discards the token), and AI features.
