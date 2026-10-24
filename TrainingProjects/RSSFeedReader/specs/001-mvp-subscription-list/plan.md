# Implementation Plan: MVP Feed Subscription List

**Branch**: `001-mvp-subscription-list` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-mvp-subscription-list/spec.md`

## Summary

Deliver the MVP: a user adds a feed subscription by URL and sees the updated list immediately. An ASP.NET Core Web API stores subscriptions in memory behind an interface and enforces all validation (empty, length, http/https only, duplicates) server-side. A Blazor WebAssembly page offers one labelled input, an Add button, inline messages, and the list. No feed fetching or parsing is included. See [research.md](research.md) for decisions.

## Technical Context

**Language/Version**: C# on .NET 10 (LTS; SDK 10.0.402 installed), nullable reference types enabled

**Primary Dependencies**: ASP.NET Core (Minimal API), Blazor WebAssembly (standalone); no third-party runtime packages. Test-only: xUnit, `Microsoft.AspNetCore.Mvc.Testing`, bUnit

**Storage**: In-memory, thread-safe list behind `ISubscriptionStore` (lost on restart, per spec FR-013)

**Testing**: xUnit; `WebApplicationFactory` integration tests (API); unit tests (validator, store); bUnit (UI page)

**Target Platform**: Windows, macOS, Linux; current desktop and mobile browsers; local single-user

**Project Type**: Web application (API backend + Blazor WebAssembly frontend + shared contracts library)

**Performance Goals**: New subscription visible within 1 s of a successful add (SC-002); 100 subscriptions in a session remain listed in order (SC-004)

**Constraints**: In-memory only; no outbound network calls from the API; POST body limited to 4 KB; URL limit 2,048 characters; CORS limited to configured frontend origins; zero build warnings

**Scale/Scope**: One user, one page, two endpoints, one entity

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. MVP-First, Scope Discipline | Only add and list; in-memory; no fetching, parsing, or outbound `HttpClient` in the API | PASS. The UI's `HttpClient` only calls the project's own API. |
| II. Security by Default | Server-side validation; http/https only; length cap; plain-text rendering; no `MarkupString`; CORS allowlist without wildcards; `ProblemDetails` without internals; no secrets; vulnerable-package check | PASS. Planned in research items 4, 5, 8, 11 and contracts. SSRF controls deferred to the fetching feature, which does not exist yet. |
| III. Code Quality & Maintainability | `TreatWarningsAsErrors`, `.editorconfig`, `dotnet format` check; frontend and backend separated and communicating over HTTP; store behind an interface with DI; DTOs at the API boundary | PASS. Research items 3, 7, 10. |
| IV. Testing Standards | Test per user-facing behavior; integration tests with `WebApplicationFactory`; unit tests including invalid and boundary inputs; bUnit tests; no external network | PASS. Test projects included in the structure. |
| V. Consistent User Experience | Immediate update; readable errors; accessible, labelled form; simple UI | PASS. See [ui-contract.md](contracts/ui-contract.md). |
| VI. Cross-Platform & Reproducible Dev | .NET SDK only; ports and URLs in configuration; demo pages removed and routing verified first | PASS. See [quickstart.md](quickstart.md) checklist. |
| Technology Constraints | .NET LTS, pinned package versions, nuget.org only | PASS |

**Post-design re-check**: No new violations. The shared contracts library and two test projects add projects beyond a minimal single-project layout; each is required by the constitution (separation, shared DTOs, testing), so no entry is needed under Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-mvp-subscription-list/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── subscriptions-api.yaml   # HTTP contract
│   └── ui-contract.md           # Page behavior and messages
├── checklists/
│   └── requirements.md
└── tasks.md             # Created later by /speckit-tasks
```

### Source Code (repository root)

```text
RSSFeedReader.slnx
Directory.Build.props        # Nullable, TreatWarningsAsErrors, code-style enforcement
.editorconfig

shared/
└── RSSFeedReader.Shared/
    └── Contracts/           # AddSubscriptionRequest, SubscriptionDto, SubscriptionLimits (MaxUrlLength)

backend/
└── RSSFeedReader.Api/
    ├── Program.cs           # DI, CORS, ProblemDetails, security header, endpoint mapping
    ├── Endpoints/           # Subscriptions endpoint group (/api/subscriptions)
    ├── Subscriptions/       # ISubscriptionStore, InMemorySubscriptionStore, FeedUrlValidator
    ├── Properties/launchSettings.json   # port 5151
    └── appsettings.json     # Cors:AllowedOrigins

frontend/
└── RSSFeedReader.UI/
    ├── Program.cs           # Typed HttpClient from ApiBaseUrl
    ├── Pages/Subscriptions.razor        # only @page "/"
    ├── Services/            # ISubscriptionsApiClient, SubscriptionsApiClient
    ├── Layout/              # MainLayout, NavMenu (demo links removed)
    ├── Properties/launchSettings.json   # port 5213
    └── wwwroot/appsettings.json         # ApiBaseUrl

tests/
├── RSSFeedReader.Api.Tests/     # FeedUrlValidator + store unit tests; WebApplicationFactory integration tests
└── RSSFeedReader.UI.Tests/      # bUnit tests for the Subscriptions page
```

**Structure Decision**: Web application layout with `backend/` and `frontend/` (paths already named in `TechStack.md`), plus a `shared/` contracts library and `tests/` projects. The frontend depends only on the shared contracts and talks to the API over HTTP.

## Complexity Tracking

No constitution violations to justify.
