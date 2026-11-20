# Phase 5: Chat

"Message Owner", the inbox, sending and reading messages, and unread counts.

No new packages, no settings, no migration.

## Endpoints now working

| Endpoint | Result |
|---|---|
| `POST /api/conversations` with `{ "postId": "..." }` | 200 with the chat (existing or new). 400 own post, 404 hidden/deleted post, 409 new chat on a resolved post |
| `GET /api/conversations?page=&pageSize=` | Inbox, most recent activity first |
| `GET /api/conversations/{id}` | One chat: post, other person, last message, unread count |
| `GET /api/conversations/{id}/messages?page=&pageSize=` | Messages, **newest first** |
| `POST /api/conversations/{id}/messages` with `{ "content": "..." }` | 201 with the message |
| `POST /api/conversations/{id}/read` | 204. Marks the other person's messages as read |

All of them need a login. Only the two people in a chat can use it; anyone else gets 404.

## Rules

- One chat per post per person. Pressing "Message Owner" again opens the same chat.
- You cannot message yourself about your own post.
- New chats only on visible, active posts. Existing chats keep working after the post is
  resolved, deleted, or hidden, so the handover can still be arranged.
- The post owner sees a chat in their inbox once the first message is sent.
- Messages are trimmed and must be 1 to 2000 characters. Blank messages are rejected.
- Opening messages does **not** mark them as read. Call `POST .../read` when the chat is on screen.
- `unreadCount` counts only the other person's unread messages. `readAt` on your own messages
  shows when the other person read them.

## For the frontend

- Messages come newest first; reverse them to show oldest at the top. Page 2 loads older messages.
- There are no live updates yet: refresh the open chat and the inbox every few seconds.
- `isMine` tells you which side of the screen a bubble goes on.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 284`.
