# Phase 6: Notifications

The notification center, plus notifications for new messages and resolved posts.

No new packages, no settings, no migration.

## Endpoints now working

| Endpoint | Result |
|---|---|
| `GET /api/notifications?unreadOnly=&page=&pageSize=` | Your notifications, newest first |
| `GET /api/notifications/unread-count` | `{ "count": 3 }` for the bell badge |
| `POST /api/notifications/{id}/read` | 204. Someone else's notification is 404 |
| `POST /api/notifications/read-all` | 204 |

Each item: `id`, `type`, `title`, `message`, `isRead`, `createdAt`, `postId`, `conversationId`.
Open the conversation when `conversationId` is set, otherwise the post.

## What creates a notification

| Event | Who is notified | Type |
|---|---|---|
| A message is sent | The other person in the chat | `NewMessage` |
| A post is marked resolved | Everyone who started a chat about it | `PostResolved` |

- While a chat's notification is unread, new messages in that chat update it and move it to the
  top instead of adding more. Opening the chat (`POST /api/conversations/{id}/read`) marks it read.
- Users with `NotifyOnNewMessage` or `NotifyOnPostUpdate` turned off get none of that type
  (the settings page arrives in phase 7). Deleted accounts get none.
- A notification is saved in the same database save as the action, so you never get one without the other.
- `title` and `message` follow the `Accept-Language` header, like campus locations.

Coming later: admin action notifications (phase 8). "Similar item" notifications belong to the AI features.

## Also in this phase

Swagger no longer lists "501 Not Implemented" for endpoints that are finished.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 336`.
