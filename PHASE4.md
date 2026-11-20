# Phase 4: Images

Post images (upload, remove), the profile picture, and serving the files.

## Setup

1. Add 2 packages to `Backend/LostAndFound.API/LostAndFound.API.csproj`, next to the others:

   ```xml
   <PackageReference Include="SkiaSharp" Version="4.157.0" />
   <PackageReference Include="SkiaSharp.NativeAssets.Linux.NoDependencies" Version="4.157.0" />
   ```

   SkiaSharp (MIT license, no known vulnerabilities) reads and re-encodes images.
   The Linux package is only used when the API runs on a Linux server.

2. Nothing else. No migration, no secrets. Files are saved in `Backend/LostAndFound.API/App_Data/uploads/`,
   which Git already ignores. To use another folder, set `Storage:RootPath`.

## Endpoints now working

| Endpoint | Who | Result |
|---|---|---|
| `POST /api/posts/{postId}/images` | Owner | 201 with the new images. Form field `images`, 1 to 5 files |
| `DELETE /api/posts/{postId}/images/{imageId}` | Owner | 204 |
| `PUT /api/users/me/avatar` | Signed in | 200 with `url`. Form field `image`, 1 file |
| `GET /uploads/...` | Anyone | The image file (`image/webp`) |

## Rules

- Accepted: JPEG, PNG, WebP, up to 5 MB each, up to 5 images per post in total.
  The type is checked from the file's content, not its name.
- Every image is re-encoded as WebP: turned upright using the phone's rotation info, scaled down
  to at most 1920 pixels on the longest side, and **all metadata removed, including GPS location**.
- If any file in an upload is bad, nothing is saved. The error names the file: `errors["Images[1]"]`.
- Someone else's post, or a deleted one, returns 404. A resolved post's images cannot change (409).
- Image URLs are relative, for example `/uploads/posts/3f2c….webp`. Prefix them with the API address.
  The first image is the thumbnail on post cards.
- Uploading a new profile picture replaces and deletes the old one.

## For the frontend

```js
const form = new FormData();
for (const file of files) form.append("images", file);
await fetch(`${api}/api/posts/${postId}/images`, {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },
  body: form // do not set Content-Type yourself; the browser adds the boundary
});
```

Typical flow: create the post (`POST /api/posts`), then upload its images with the returned `id`.

## Tests

`dotnet test LostAndFound.API.Tests` in `Backend` → `Passed! - Failed: 0, Passed: 244`.
