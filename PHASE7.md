# Phase 7: Profile and support

No new packages and no migration. One new setting, `Support:InboxEmail` (see below).

## Endpoints now working

| Endpoint | Result |
|---|---|
| `GET /api/users/me` | Profile: name, email, user type, role (`User` or `Admin`), profile picture |
| `PUT /api/users/me` | Edit name, email, user type. 409 if the email is taken |
| `PUT /api/users/me/password` | 204. Needs the current password |
| `GET /api/users/me/settings` | Language and the 3 notification switches |
| `PUT /api/users/me/settings` | Replaces all 4 settings |
| `DELETE /api/users/me` with `{ "password": "..." }` | 204. Deletes the account |
| `POST /api/support-tickets` | 202. Guests and signed-in users |

## Rules

- **Changing the email needs `currentPassword`** in the same request. Changing only the name,
  user type, or the letter case of the email does not.
- A wrong password (email change, password change, account deletion) is a **400** on that field,
  not a 401, because the user is still signed in. Wrong passwords count toward the same lockout
  as login: 5 in a row lock the account for 15 minutes.
- Changing the email or the password cancels any password reset link that was already sent.
- **Deleting an account** (in one transaction, so it either fully happens or not at all):
  - Name becomes "Deleted user"; email, password, and profile picture are removed.
  - All the user's posts are hidden. All their notifications are deleted.
  - Their support tickets lose the name and email.
  - Chats stay, so the other person keeps the history; the deleted side shows "Deleted user".
  - The email address can be used to register a new account.
- **Every request now checks the account is still active.** A token for an account that was
  deleted or disabled stops working immediately, instead of lasting until it expires.
- **Support tickets** are saved, then a copy is emailed to `Support:InboxEmail` with Reply-To set
  to the sender, so the support team can answer by replying. If the email fails, the ticket is still saved.

## Setting

`appsettings.json` now has:

```json
"Support": {
  "InboxEmail": "support@findly.local"
}
```

Change it to the real support inbox before going live. The API refuses to start if it is missing or not an email.
In Development, the copy is written to `App_Data/emails/` like the reset emails.

## For the frontend

- `DELETE /api/users/me` takes a JSON body: `fetch(url, { method: "DELETE", headers: {..., "Content-Type": "application/json"}, body: JSON.stringify({ password }) })`.
- After deleting the account, or getting any 401, send the user to the login page.
- Show `errors.CurrentPassword`, `errors.NewPassword`, or `errors.Password` next to the matching field.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 387`.
Run the SQL Server check from `CHECK-ON-SQLSERVER.md` again: account deletion uses a database
transaction, which is the one part the fast SQLite run cannot fully prove on SQL Server.
