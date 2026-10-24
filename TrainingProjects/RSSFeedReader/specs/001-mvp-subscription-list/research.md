# Research: MVP Feed Subscription List

All Technical Context items were resolvable from the stakeholder documents, the constitution, and the installed toolchain (.NET SDK 10.0.402). No `NEEDS CLARIFICATION` items remain.

## 1. Runtime and framework versions

- **Decision**: Target .NET 10 (current LTS) for the Web API, the Blazor WebAssembly app, the shared contracts library, and the tests.
- **Rationale**: The constitution requires "current LTS"; the installed SDK is 10.0.402. A single target framework keeps the build simple.
- **Alternatives considered**: .NET 8 LTS (older, no benefit as the SDK is already on 10).

## 2. API style

- **Decision**: Minimal API with an endpoint group mapped at `/api/subscriptions`, using explicit request/response DTOs.
- **Rationale**: Two endpoints do not justify controllers. The `api/` base path matches the `ApiBaseUrl` convention in `TechStack.md`. DTOs satisfy the constitution rule against exposing internal entities.
- **Alternatives considered**: MVC controllers (more ceremony for two endpoints, no added value).

## 3. Storage

- **Decision**: An `ISubscriptionStore` interface with a singleton, thread-safe in-memory implementation (ordered list guarded by a lock).
- **Rationale**: The spec requires in-memory only (FR-013). The interface is the seam the constitution requires so EF Core + SQLite can replace it post-MVP. Concurrent requests are possible, so access must be synchronized. Atomic "check duplicate then add" must happen inside the store to avoid a race that would allow duplicates.
- **Alternatives considered**: Raw `List<string>` in the endpoint (violates the interface seam, unsafe under concurrency); `ConcurrentDictionary` (loses insertion order).

## 4. Validation and normalization

- **Decision**: A single server-side `FeedUrlValidator` that (1) trims whitespace, (2) rejects empty values, (3) rejects values longer than 2,048 characters, (4) requires `Uri.TryCreate(..., UriKind.Absolute)` with scheme `http` or `https` and a non-empty host, and (5) produces a normalized comparison key. The stored/displayed value is the trimmed text as entered.
- **Duplicate key**: lowercase scheme and host (the `Uri` type already does this), explicit default ports removed, fragment ignored, and a single trailing `/` removed from the path. Path and query otherwise remain case-sensitive, because they are case-sensitive on most servers.
- **Rationale**: Satisfies FR-005 to FR-008 and the Edge Cases in the spec. Server-side enforcement is required by FR-008 and constitution Principle II. The UI may mirror the empty check for responsiveness but is never the only check.
- **Alternatives considered**: Regex validation (error-prone); data annotations on the DTO (cannot express normalization and duplicate logic cleanly); rejecting URLs containing user-info (`user:pass@host`) — rejected as out of scope for MVP, but see risk note below.
- **Risk note**: URLs with embedded credentials are syntactically valid and would be displayed as text. No fetching happens in the MVP, so there is no SSRF exposure. SSRF controls (constitution Principle II) are deferred to the Extended-MVP feature that introduces fetching.

## 5. HTTP contract and error handling

- **Decision**:
  - `GET /api/subscriptions` returns `200` with a JSON array in insertion order.
  - `POST /api/subscriptions` with `{ "url": "..." }` returns `201 Created` plus the created subscription, `400` with an RFC 9457 `ProblemDetails` validation body for invalid input, or `409` with `ProblemDetails` for duplicates.
  - Unhandled exceptions return a generic `500` `ProblemDetails` with no stack trace (`UseExceptionHandler` + `AddProblemDetails`).
- **Rationale**: Matches constitution Principle II (no internal details in errors) and gives the UI distinct, user-readable cases for FR-006, FR-007, and FR-011.
- **Alternatives considered**: Always returning `200` with an error flag (non-standard, harder to test).

## 6. Frontend approach

- **Decision**: Blazor WebAssembly standalone app with one page (`@page "/"`, title "Subscriptions") containing a labelled text input, an Add button, an inline message area, and the list. API calls go through a small `SubscriptionsApiClient` wrapping a typed `HttpClient` whose base address comes from `wwwroot/appsettings.json` (`ApiBaseUrl`). Feed addresses render as plain text via normal Razor interpolation.
- **Rationale**: Matches `TechStack.md` and Principles II, V, VI. A client wrapper makes the page testable with bUnit by substituting the client interface.
- **Alternatives considered**: Calling `HttpClient` directly from the component (harder to test, mixes concerns); a state-management library (not justified).

## 7. Shared contracts

- **Decision**: A small `RSSFeedReader.Shared` class library holding the two DTOs (`AddSubscriptionRequest`, `SubscriptionDto`) and the `MaxUrlLength` constant used by both sides.
- **Rationale**: Prevents DTO drift between frontend and backend; permitted by constitution Principle III and by `TechStack.md` ("Shared Code").
- **Alternatives considered**: Duplicated DTOs in each project (drift risk).

## 8. Cross-origin access

- **Decision**: A named CORS policy allowing only the origins listed in backend configuration (`Cors:AllowedOrigins`), defaulting to `http://localhost:5213` and `https://localhost:7025`; methods limited to `GET` and `POST`, headers limited to `Content-Type`.
- **Rationale**: Constitution forbids wildcard origins; reading from configuration keeps ports consistent in one place (Principle VI).
- **Alternatives considered**: `AllowAnyOrigin` (forbidden).

## 9. Testing tools

- **Decision**: xUnit for all tests. `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for API integration tests, plain unit tests for `FeedUrlValidator` and the store, and bUnit for the Blazor page.
- **Rationale**: Directly satisfies constitution Principle IV; all are deterministic and need no network.
- **Alternatives considered**: Playwright end-to-end tests (valuable later; excessive for the MVP and slower, so not required here; the quickstart provides a manual end-to-end check).

## 10. Quality gates and tooling

- **Decision**: A root `Directory.Build.props` enabling `Nullable`, `TreatWarningsAsErrors`, and `EnforceCodeStyleInBuild`; a root `.editorconfig`; `dotnet format --verify-no-changes` and `dotnet list package --vulnerable --include-transitive` as pre-merge commands. Package versions are pinned explicitly.
- **Rationale**: Directly implements Principle III and the Security & Technology Constraints section.
- **Alternatives considered**: Adding third-party analyzers (not needed yet; built-in .NET analyzers suffice).

## 11. Security headers

- **Decision**: A small middleware on the API sets `X-Content-Type-Options: nosniff`. Request body size for the POST endpoint is capped at a small limit (4 KB) so oversized payloads fail before parsing.
- **Rationale**: Cheap hardening aligned with the constitution; the cap supports the 2,048-character limit without reading huge bodies.
- **Alternatives considered**: Full header suite (HSTS, CSP) — deferred; the API serves only JSON on localhost in the MVP.
