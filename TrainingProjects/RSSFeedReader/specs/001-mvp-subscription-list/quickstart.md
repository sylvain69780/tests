# Quickstart: Validating the MVP Subscription List

Run from the repository root (`TrainingProjects/RSSFeedReader`). Commands work in PowerShell, bash, and zsh unless noted.

## Prerequisites

- .NET SDK 10 (`dotnet --version` reports 10.x)
- A current browser
- Ports 5151 (API) and 5213 (UI) free

## 1. Build, format, and test (quality gates)

```text
dotnet build
dotnet format --verify-no-changes
dotnet test
dotnet list package --vulnerable --include-transitive
```

Expected: zero warnings, no formatting changes, all tests pass, no vulnerable packages.

## 2. Run the API

```text
dotnet run --project backend/RSSFeedReader.Api
```

Expected: listens on `http://localhost:5151`.

## 3. Check the API contract

See [subscriptions-api.yaml](contracts/subscriptions-api.yaml).

| Request | Expected |
|---------|----------|
| `GET /api/subscriptions` (fresh start) | `200`, `[]` |
| `POST /api/subscriptions` body `{"url":"  https://devblogs.microsoft.com/dotnet/feed/  "}` | `201`, body `{"url":"https://devblogs.microsoft.com/dotnet/feed/"}` |
| Same POST again | `409` |
| `POST` with `{"url":"HTTPS://DevBlogs.Microsoft.com/dotnet/feed"}` | `409` (same normalized key) |
| `POST` with `{"url":""}` | `400` |
| `POST` with `{"url":"not a url"}` or `{"url":"javascript:alert(1)"}` | `400` |
| `POST` with a 2,049-character URL | `400` |
| `GET /api/subscriptions` | `200` with exactly one entry |

## 4. Run the UI

In a second terminal:

```text
dotnet run --project frontend/RSSFeedReader.UI
```

Open `http://localhost:5213`.

## 5. Manual end-to-end walkthrough (maps to spec scenarios)

1. Page loads showing the empty-state message (US2-1). DevTools console shows no connection or CORS errors.
2. Paste `https://devblogs.microsoft.com/dotnet/feed/`, press Add: it appears in the list immediately, input is cleared (US1-1, US2-2, SC-002).
3. Add a second, different address: both are listed in the order added (US1-2).
4. Submit an empty input, then `not a url`, then `javascript:alert(1)`: each shows a message, the list is unchanged, and typed text stays in the input (US1-3, US3-1, US3-2).
5. Re-submit the first address: "You are already subscribed to this feed." (US3-3).
6. Stop and restart the API, reload the page: empty-state message again (expected in-memory behavior).
7. Stop the API and press Add: the "Could not add the subscription" message appears (FR-011).

## 6. Local configuration checklist

- [ ] `backend/RSSFeedReader.Api/Properties/launchSettings.json` uses port 5151
- [ ] `frontend/RSSFeedReader.UI/Properties/launchSettings.json` uses port 5213
- [ ] `frontend/RSSFeedReader.UI/wwwroot/appsettings.json` has `ApiBaseUrl` = `http://localhost:5151/api/`
- [ ] Backend `Cors:AllowedOrigins` includes `http://localhost:5213`
- [ ] Only one page declares `@page "/"` (no `Home.razor`, `Counter.razor`, `Weather.razor`)
