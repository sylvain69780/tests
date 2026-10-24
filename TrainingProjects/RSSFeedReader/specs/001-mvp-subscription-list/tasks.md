---

description: "Task list for the MVP Feed Subscription List"
---

# Tasks: MVP Feed Subscription List

**Input**: Design documents from `/specs/001-mvp-subscription-list/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Included. Constitution Principle IV makes automated tests mandatory for every user-facing behavior. Write each story's tests first and confirm they fail before implementing.

**Organization**: Tasks are grouped by user story. US1 (add) and US2 (list) are both P1 and together form "MVP working"; US3 (input feedback) is P2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story the task belongs to (US1, US2, US3)
- All paths are relative to the repository root `TrainingProjects/RSSFeedReader/`

## Path Conventions

Web application per plan: `shared/RSSFeedReader.Shared/`, `backend/RSSFeedReader.Api/`, `frontend/RSSFeedReader.UI/`, `tests/RSSFeedReader.Api.Tests/`, `tests/RSSFeedReader.UI.Tests/`. Target framework `net10.0` everywhere. Pin every NuGet package to an explicit version.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the solution and empty projects so everything builds.

- [X] T001 Create the solution file `RSSFeedReader.slnx` at the repository root with `dotnet new sln --format slnx -n RSSFeedReader`
- [X] T002 Create `Directory.Build.props` at the repository root setting `TargetFramework` net10.0, `Nullable` enable, `ImplicitUsings` enable, `TreatWarningsAsErrors` true, `EnforceCodeStyleInBuild` true
- [X] T003 [P] Create `.editorconfig` at the repository root with standard C# naming and formatting rules (file-scoped namespaces, 4-space indent, `_camelCase` private fields, `var` where type is apparent) enforced as warnings so `dotnet format --verify-no-changes` is meaningful
- [X] T004 [P] Create class library `shared/RSSFeedReader.Shared/RSSFeedReader.Shared.csproj` (no dependencies)
- [X] T005 [P] Create Minimal API project `backend/RSSFeedReader.Api/` (`dotnet new web`), reference the Shared project, and set `Properties/launchSettings.json` to `http://localhost:5151` (remove other profiles/ports)
- [X] T006 [P] Create standalone Blazor WebAssembly project `frontend/RSSFeedReader.UI/` (`dotnet new blazorwasm`), reference the Shared project, and set `Properties/launchSettings.json` to `http://localhost:5213` and `https://localhost:7025`
- [X] T007 [P] Create xUnit project `tests/RSSFeedReader.Api.Tests/` referencing the Api and Shared projects, with package `Microsoft.AspNetCore.Mvc.Testing`
- [X] T008 [P] Create xUnit project `tests/RSSFeedReader.UI.Tests/` referencing the UI and Shared projects, with package `bunit`
- [X] T009 Add all five projects to `RSSFeedReader.slnx` and confirm `dotnet build` succeeds with zero warnings (depends on T001-T008)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared contracts, hardened API host, and a clean frontend. No user story can start until this phase completes.

- [X] T010 [P] Create shared contracts in `shared/RSSFeedReader.Shared/Contracts/`: `AddSubscriptionRequest.cs` (`record AddSubscriptionRequest(string? Url)`), `SubscriptionDto.cs` (`record SubscriptionDto(string Url)`), and `SubscriptionLimits.cs` (`public const int MaxUrlLength = 2048;`)
- [X] T011 [P] Clean the Blazor template in `frontend/RSSFeedReader.UI/`: delete `Pages/Home.razor`, `Pages/Counter.razor`, `Pages/Weather.razor` and `wwwroot/sample-data/`; remove their links from `Layout/NavMenu.razor` leaving a single link "Subscriptions" to `/`; then run `dotnet clean` and `dotnet build` on the UI project and list `Pages/*.razor` to confirm none of the three remain (constitution Principle VI)
- [X] T012 Configure `backend/RSSFeedReader.Api/Program.cs`: `AddProblemDetails()` and `UseExceptionHandler()` so unhandled errors return a generic 500 `ProblemDetails` with no stack trace; middleware adding `X-Content-Type-Options: nosniff`; a named CORS policy built from configuration `Cors:AllowedOrigins` allowing only methods GET and POST and header `Content-Type` (no wildcard origins); and `public partial class Program { }` at the end so `WebApplicationFactory` can use it
- [X] T013 [P] Add `Cors:AllowedOrigins` = `["http://localhost:5213", "https://localhost:7025"]` to `backend/RSSFeedReader.Api/appsettings.json`
- [X] T014 [P] Create `frontend/RSSFeedReader.UI/wwwroot/appsettings.json` with `{ "ApiBaseUrl": "http://localhost:5151/api/" }` and `frontend/RSSFeedReader.UI/Services/ISubscriptionsApiClient.cs` declaring `Task<AddSubscriptionOutcome> AddAsync(string url, CancellationToken ct = default)` and `Task<IReadOnlyList<SubscriptionDto>?> ListAsync(CancellationToken ct = default)` (returns null when loading fails), plus `record AddSubscriptionOutcome(bool Succeeded, string? ErrorMessage)`
- [X] T015 Register in `frontend/RSSFeedReader.UI/Program.cs` a typed `HttpClient` for `ISubscriptionsApiClient` whose `BaseAddress` is read from configuration key `ApiBaseUrl` (no hard-coded URL in components); remove the template's default `HttpClient` registration (depends on T014)

**Checkpoint**: `dotnet build` is clean; API starts on 5151; the UI builds with no demo pages.

---

## Phase 3: User Story 1 - Add a feed subscription by URL (Priority: P1) 🎯 MVP

**Goal**: A user pastes a feed address, confirms, and the subscription is stored; empty input is rejected with a clear message.

**Independent Test**: Start the API and UI, paste `https://devblogs.microsoft.com/dotnet/feed/`, press Add: it is accepted and the input is cleared. Submit an empty input: a "A feed address is required." message appears and nothing is stored.

### Tests for User Story 1 (write first, confirm failing)

- [X] T016 [P] [US1] Unit tests for `FeedUrlValidator` trimming and the empty rule in `tests/RSSFeedReader.Api.Tests/Unit/FeedUrlValidatorTests.cs`: null, `""`, and whitespace-only are invalid with message "A feed address is required."; `"  https://example.com/feed  "` is valid and its stored value is trimmed (FR-005, FR-006)
- [X] T017 [P] [US1] Unit tests for `InMemorySubscriptionStore.TryAdd` in `tests/RSSFeedReader.Api.Tests/Unit/InMemorySubscriptionStoreTests.cs`: added subscriptions are kept, and 100 concurrent adds of distinct addresses all succeed with none lost (SC-004)
- [X] T018 [P] [US1] Integration tests for `POST /api/subscriptions` in `tests/RSSFeedReader.Api.Tests/Integration/AddSubscriptionTests.cs` using `WebApplicationFactory<Program>`: valid URL with surrounding spaces returns 201 and body `{"url":"<trimmed>"}`; two different URLs are both stored (assert through `ISubscriptionStore` resolved from `factory.Services`); empty URL returns 400 `application/problem+json` with `errors.url[0]` = "A feed address is required."; missing body returns 400
- [X] T019 [P] [US1] bUnit tests for the add flow in `tests/RSSFeedReader.UI.Tests/SubscriptionsPageAddTests.cs` with a fake `ISubscriptionsApiClient`: the input has the visible label "Feed address"; clicking Add (and pressing Enter) calls `AddAsync` with the typed text; on success the input is cleared; on failure the typed text is kept and the message appears in an element with `role="alert"`; the Add button is disabled while the call is pending (FR-009)

### Implementation for User Story 1

- [X] T020 [P] [US1] Create `backend/RSSFeedReader.Api/Subscriptions/FeedUrlValidator.cs` with a result type (`IsValid`, `Error`, trimmed `Url`, `Key`): trim whitespace (FR-005), reject null/empty with "A feed address is required."; for now `Key` equals the trimmed value (normalization arrives in US3)
- [X] T021 [P] [US1] Create `backend/RSSFeedReader.Api/Subscriptions/ISubscriptionStore.cs` (`AddResult TryAdd(string url, string key)`) and `InMemorySubscriptionStore.cs`: ordered `List` guarded by a `lock`, append on add, singleton-safe (per data-model: ordered by insertion, no id)
- [X] T022 [US1] Create `backend/RSSFeedReader.Api/Endpoints/SubscriptionsEndpoints.cs` mapping group `/api/subscriptions` with `POST` taking `AddSubscriptionRequest`: validate via `FeedUrlValidator`, on error return `Results.ValidationProblem` with `errors = { "url": [message] }`, on success return `201 Created` with `SubscriptionDto`; apply `[RequestSizeLimit(4096)]`; register the store as a singleton and map the group in `backend/RSSFeedReader.Api/Program.cs` (depends on T012, T020, T021)
- [X] T023 [P] [US1] Create `frontend/RSSFeedReader.UI/Services/SubscriptionsApiClient.cs` implementing `AddAsync`: POST JSON to `subscriptions`; 201 returns success; 400 returns the first `errors.url` message from the `ProblemDetails` body; network failure or any other status returns "Could not add the subscription. Please try again." and never exposes raw exception text (FR-011) (depends on T014)
- [X] T024 [US1] Create `frontend/RSSFeedReader.UI/Pages/Subscriptions.razor` with `@page "/"`: a form with `<label>` "Feed address" bound to a text input, an Add button (Enter submits), an alert region (`role="alert"`) for the latest error, button disabled while pending, input cleared only on success; no `MarkupString` (depends on T023)
- [X] T025 [US1] Run the US1 tests until green, confirm only one file declares `@page "/"` (`Get-ChildItem frontend/RSSFeedReader.UI -Recurse -Filter *.razor | Select-String '@page'`), then start API and UI and perform quickstart walkthrough steps 2 and 4 (empty input only)

**Checkpoint**: A subscription can be added and empty input is rejected; the story is testable on its own.

---

## Phase 4: User Story 2 - See the list of subscriptions (Priority: P1)

**Goal**: The UI shows all subscriptions in the order added, updates immediately after an add, and shows an empty-state message when there are none.

**Independent Test**: Open the app with no subscriptions and see "No subscriptions yet. Add a feed address above."; add one and see it appear without reloading; reload after adding to see it still listed (until the API restarts).

### Tests for User Story 2 (write first, confirm failing)

- [X] T026 [P] [US2] Add store tests to `tests/RSSFeedReader.Api.Tests/Unit/InMemorySubscriptionStoreTests.cs` for `GetAll`: empty store returns an empty list; results are in insertion order; the returned list is a snapshot (later adds do not alter an already returned list)
- [X] T027 [P] [US2] Integration tests for `GET /api/subscriptions` in `tests/RSSFeedReader.Api.Tests/Integration/ListSubscriptionsTests.cs`: fresh factory returns 200 `[]`; after posting A then B returns `[A, B]` in order; 100 posted subscriptions are all returned in order (SC-004)
- [X] T028 [P] [US2] bUnit tests for listing in `tests/RSSFeedReader.UI.Tests/SubscriptionsPageListTests.cs`: empty state message shown when the client returns an empty list; entries rendered in order; a successful add shows the new entry without reload; a value such as `<b>x</b>` is rendered HTML-encoded (not as markup) (FR-010); when `ListAsync` returns null the message "Could not load your subscriptions. Please try again." appears in the alert region (FR-011)

### Implementation for User Story 2

- [X] T029 [US2] Add `IReadOnlyList<SubscriptionDto> GetAll()` to `backend/RSSFeedReader.Api/Subscriptions/ISubscriptionStore.cs` and implement it in `InMemorySubscriptionStore.cs` returning a copy taken under the lock
- [X] T030 [US2] Map `GET /api/subscriptions` in `backend/RSSFeedReader.Api/Endpoints/SubscriptionsEndpoints.cs` returning 200 with the store's `GetAll()` (depends on T029)
- [X] T031 [P] [US2] Implement `ListAsync` in `frontend/RSSFeedReader.UI/Services/SubscriptionsApiClient.cs`: GET `subscriptions`, return the list, or null on any failure
- [X] T032 [US2] Update `frontend/RSSFeedReader.UI/Pages/Subscriptions.razor`: load the list in `OnInitializedAsync`; render entries in an ordered list as plain text; show the empty-state message when empty; after a successful add reload the list so it updates immediately (SC-002); show the load-failure message on null (depends on T031)
- [X] T033 [US2] Run all tests until green, then perform quickstart walkthrough steps 1-3 and 6 manually

**Checkpoint**: US1 and US2 together satisfy both "MVP working" criteria from `ProjectGoals.md`.

---

## Phase 5: User Story 3 - Receive clear feedback on unusable input (Priority: P2)

**Goal**: Malformed, unsupported-scheme, over-long, and duplicate addresses are rejected with a readable message and no list change.

**Independent Test**: Submit `not a url`, `javascript:alert(1)`, a 2,049-character address, and an already-added address: each is rejected with its message and the list is unchanged.

### Tests for User Story 3 (write first, confirm failing)

- [X] T034 [P] [US3] Extend `tests/RSSFeedReader.Api.Tests/Unit/FeedUrlValidatorTests.cs`: 2,048 characters valid and 2,049 invalid ("The feed address is too long (maximum 2,048 characters)."); `not a url`, `example.com/feed`, `http://`, `javascript:alert(1)`, `file:///c:/x`, `ftp://example.com/f`, `data:text/plain,hi` all invalid with "Enter a valid http or https web address."; normalized keys are equal for `https://Example.com/feed`, `https://example.com/feed/`, `https://example.com:443/feed#top`, and different for `https://example.com/feed` vs `https://example.com/Feed` and `http://` vs `https://` (data-model Normalized key)
- [X] T035 [P] [US3] Extend `tests/RSSFeedReader.Api.Tests/Unit/InMemorySubscriptionStoreTests.cs`: adding a second item with the same key returns a duplicate result and does not change the list; 50 concurrent adds of the same key result in exactly one stored item
- [X] T036 [P] [US3] Extend `tests/RSSFeedReader.Api.Tests/Integration/AddSubscriptionTests.cs`: too long and non-web addresses return 400 with the matching `errors.url[0]` message and leave GET unchanged; a duplicate (including `HTTPS://DevBlogs.Microsoft.com/dotnet/feed` vs the already added `https://devblogs.microsoft.com/dotnet/feed/`) returns 409 `ProblemDetails` with detail "You are already subscribed to this feed."; a body over 4 KB is rejected with 413
- [X] T037 [P] [US3] Extend `tests/RSSFeedReader.UI.Tests/SubscriptionsPageAddTests.cs`: the 400 and 409 messages from the client are shown in the alert region; typed text is kept after each rejection; the alert clears on the next submit; the input has `maxlength` 2048

### Implementation for User Story 3

- [X] T038 [US3] Complete `backend/RSSFeedReader.Api/Subscriptions/FeedUrlValidator.cs` after the empty rule: reject length above `SubscriptionLimits.MaxUrlLength`; require `Uri.TryCreate(value, UriKind.Absolute, out var uri)` with scheme `http` or `https` and non-empty host; compute `Key` as lowercase scheme and host, default port omitted, fragment removed, one trailing `/` removed from the path, query kept, path and query case preserved; error messages exactly as in [data-model.md](data-model.md)
- [X] T039 [US3] Make `TryAdd` in `backend/RSSFeedReader.Api/Subscriptions/InMemorySubscriptionStore.cs` check for an existing item with the same key and append inside the same lock, returning a duplicate result (store keys alongside the trimmed URL); update `AddResult` in `ISubscriptionStore.cs` accordingly
- [X] T040 [US3] In `backend/RSSFeedReader.Api/Endpoints/SubscriptionsEndpoints.cs` map a duplicate result to `Results.Problem(statusCode: 409, title: "Already subscribed", detail: "You are already subscribed to this feed.")` (depends on T038, T039)
- [X] T041 [P] [US3] Extend `frontend/RSSFeedReader.UI/Services/SubscriptionsApiClient.cs` `AddAsync` so a 409 returns the `ProblemDetails` `detail` text as the error message
- [X] T042 [US3] Set `maxlength="@SubscriptionLimits.MaxUrlLength"` on the input in `frontend/RSSFeedReader.UI/Pages/Subscriptions.razor` and verify rejected submissions keep the typed text (depends on T041)
- [X] T043 [US3] Run all tests until green, then perform quickstart walkthrough steps 4 and 5 manually

**Checkpoint**: All three stories work independently and together.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T044 [P] Add API tests in `tests/RSSFeedReader.Api.Tests/Integration/HostHardeningTests.cs`: responses carry `X-Content-Type-Options: nosniff`; a request from an allowed origin gets a matching `Access-Control-Allow-Origin` and one from `http://evil.example` gets none (no wildcard); a forced unhandled exception returns a 500 `ProblemDetails` body without a stack trace
- [X] T045 [P] Search the repository for `MarkupString`, `@((MarkupString`, `AllowAnyOrigin`, and hard-coded `localhost:5151` in `frontend/RSSFeedReader.UI/**/*.razor` and `*.cs`; remove any finding
- [X] T046 Run `dotnet format --verify-no-changes`, `dotnet build` (zero warnings), `dotnet test`, and `dotnet list package --vulnerable --include-transitive`; fix every finding
- [X] T047 Execute the full [quickstart.md](quickstart.md) (steps 1-6 including the configuration checklist) and confirm every expected outcome, including the API-down case in walkthrough step 7

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 and T002 first; T003-T008 in parallel after them; T009 last.
- **Foundational (Phase 2)**: needs Phase 1. T010, T011, T013, T014 in parallel; T012 after T005; T015 after T014 and T011. Blocks all user stories.
- **US1 (Phase 3)**: needs Phase 2. **US2 (Phase 4)**: needs Phase 2 and builds on US1's store, endpoint file, client, and page, so it follows US1. **US3 (Phase 5)**: needs US1 (it extends the validator, store, endpoint, client, and page).
- **Polish (Phase 6)**: after all desired stories.

### Within Each User Story

Tests first (failing), then validator/store, then endpoint, then client, then page, then the verification task.

### Story Completion Order

```text
Setup -> Foundational -> US1 -> US2 -> US3 -> Polish
```

US2 and US3 touch the same files (`SubscriptionsEndpoints.cs`, `SubscriptionsApiClient.cs`, `Subscriptions.razor`, `InMemorySubscriptionStore.cs`), so run them sequentially rather than in parallel.

## Parallel Opportunities

- Setup: T003, T004, T005, T006, T007, T008
- Foundational: T010, T011, T013, T014
- US1 tests: T016, T017, T018, T019; then T020, T021, T023 together
- US2 tests: T026, T027, T028; T031 alongside T029
- US3 tests: T034, T035, T036, T037
- Polish: T044, T045

### Parallel example: User Story 1 tests

```text
T016 FeedUrlValidator tests   (tests/RSSFeedReader.Api.Tests/Unit/FeedUrlValidatorTests.cs)
T017 Store tests              (tests/RSSFeedReader.Api.Tests/Unit/InMemorySubscriptionStoreTests.cs)
T018 POST integration tests   (tests/RSSFeedReader.Api.Tests/Integration/AddSubscriptionTests.cs)
T019 bUnit add-flow tests     (tests/RSSFeedReader.UI.Tests/SubscriptionsPageAddTests.cs)
```

## Implementation Strategy

### MVP First

1. Complete Setup and Foundational phases.
2. Complete US1 (add) and US2 (list). Stop and validate: both "MVP working" criteria from `ProjectGoals.md` are met and demonstrable.
3. Add US3 for input safety (required by constitution Principle II before the MVP is considered done) and finish with Polish.

### Notes

- Commit after each task or logical group; keep PRs small.
- Do not add feed fetching, parsing, persistence, or removal (out of scope for this feature).
- Never render feed addresses as markup; keep error messages free of technical details.
