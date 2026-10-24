# RSS Feed Reader Constitution

## Core Principles

### I. MVP-First, Scope Discipline

Work MUST follow the phases in `StakeholderDocuments/ProjectGoals.md`: MVP (add a subscription by
URL, list subscriptions), then Extended-MVP (manual refresh, title + link display), then post-MVP.

- A change MUST NOT implement a feature from a later phase until the current phase is working and
  verified against its "MVP working" criteria.
- MVP storage MUST be in-memory only; MVP MUST NOT include feed fetching, parsing, or an
  `HttpClient` to external feeds.
- New dependencies, layers, and abstractions MUST be justified by a requirement of the current
  phase (YAGNI). Unjustified complexity MUST be rejected in review.

Rationale: This is a proof-of-concept whose value is a fast, clean demonstration; scope creep
destroys that value.

### II. Security by Default (NON-NEGOTIABLE)

All input is untrusted, including subscription URLs and, from Extended-MVP onward, remote feed
content.

- The backend MUST validate every API request server-side; client-side checks are never sufficient.
- Subscription URLs MUST be non-empty, within a bounded length, and an absolute `http` or `https`
  URI. Schemes such as `file`, `javascript`, and `data` MUST be rejected. Verifying that a URL is a
  reachable or well-formed feed remains out of MVP scope.
- When feed fetching is introduced, the server MUST block requests to loopback, link-local, and
  private network addresses (SSRF), MUST set request timeouts and maximum response sizes, and MUST
  limit redirects.
- Feed titles, links, and content MUST be rendered as plain text (Blazor default encoding).
  `MarkupString` or raw HTML rendering MUST NOT be used until a sanitization approach is approved
  via amendment of this principle's guidance.
- Links displayed from feed data MUST be restricted to `http`/`https` before being rendered as
  anchors.
- Secrets, credentials, and tokens MUST NOT be committed to source control or hard-coded; use
  user secrets or environment configuration.
- CORS MUST allow only the explicitly configured frontend origins; wildcard origins MUST NOT be
  used.
- Error responses MUST NOT expose stack traces or internal details; use `ProblemDetails`.
- NuGet dependencies MUST be reviewed for known vulnerabilities (`dotnet list package
  --vulnerable`) before each release and when dependencies change.

Rationale: Even a local POC handles user-supplied URLs and, later, internet content; secure habits
are cheaper to build in than to retrofit.

### III. Code Quality & Maintainability

- Code MUST build with zero warnings in CI/local builds (`TreatWarningsAsErrors` enabled) with
  nullable reference types enabled.
- Code MUST follow the repository `.editorconfig` and standard C# naming conventions; formatting
  MUST pass `dotnet format --verify-no-changes`.
- The solution MUST keep the backend (`backend/RSSFeedReader.Api`) and frontend
  (`frontend/RSSFeedReader.UI`) separated; the frontend MUST communicate with the backend only
  through the HTTP API. Shared contracts (DTOs) MAY live in a shared project.
- Business logic MUST sit behind an interface (e.g., a subscription store) registered through
  dependency injection, so in-memory storage can be replaced by EF Core + SQLite without changing
  callers.
- Public API endpoints MUST use explicit request/response DTOs, not internal entities.
- Methods SHOULD stay small and single-purpose; comments MUST explain "why" only where intent is
  not obvious from the code.
- Dead code, commented-out code, and unused template files MUST NOT be committed.

Rationale: A clean seam between storage, API, and UI is what lets the POC grow into the
post-MVP features without a rewrite.

### IV. Testing Standards

- Every user-facing behavior in a feature spec MUST have at least one automated test before the
  feature is considered done.
- Backend: API behavior MUST be covered by integration tests using
  `WebApplicationFactory`; logic with branching (e.g., URL validation) MUST have unit tests that
  include invalid and boundary inputs.
- Frontend: components with logic MUST be covered with bUnit tests (add-subscription flow, list
  rendering, error display).
- Tests MUST be deterministic and MUST NOT call real external feeds; Extended-MVP tests use
  fixture feed documents or a stubbed `HttpMessageHandler`. The known-good feed
  `https://devblogs.microsoft.com/dotnet/feed/` MAY be used for manual verification only.
- A bug fix MUST include a regression test that fails without the fix.
- All tests MUST pass before merge.

Rationale: Fast, deterministic tests are the only reliable guard for a fast-moving demo project.

### V. Consistent User Experience

- The UI MUST update immediately after a successful add; the subscription list MUST reflect
  backend state.
- Failures MUST surface as clear, user-readable messages (Extended-MVP: "Failed to load feed");
  silent failures and raw exception text MUST NOT be shown.
- The UI MUST be keyboard accessible, use semantic HTML and labelled form inputs, and remain
  usable at common desktop and mobile widths.
- The UI MUST stay simple and functional; visual polish is secondary to correctness.

Rationale: Predictable, accessible behavior matters more than polish for a subscription-management
demonstration.

### VI. Cross-Platform & Reproducible Local Development

- The app MUST build, run, and pass tests on Windows, macOS, and Linux using only the .NET SDK
  (ASP.NET Core Web API + Blazor WebAssembly).
- Ports, API base URL, and CORS origins MUST be defined in configuration
  (`launchSettings.json`, `wwwroot/appsettings.json`, backend `Program.cs`) and kept consistent;
  the API URL MUST NOT be hard-coded in components.
- After creating the Blazor project, template demo pages (`Home.razor`, `Counter.razor`,
  `Weather.razor`) MUST be removed, navigation updated, and routing verified (only one
  `@page "/"`) before any UI feature work begins.
- Scripts and commands in documentation MUST NOT assume a single operating system or shell
  without an equivalent alternative.

Rationale: Routing and port misconfiguration are the most common avoidable failures noted in
`StakeholderDocuments/TechStack.md`.

## Security & Technology Constraints

- Stack: .NET (current LTS), ASP.NET Core Web API backend, Blazor WebAssembly frontend. Extended-MVP
  feed parsing MUST use `System.ServiceModel.Syndication`; persistence, when introduced, MUST use
  EF Core + SQLite with parameterized access only (no string-built SQL); background polling, when
  introduced, MUST use `BackgroundService`.
- Third-party packages MUST be limited to those required by the current phase, pinned to explicit
  versions, and from trusted sources (nuget.org).
- HTTPS MUST be used for any non-localhost deployment; security headers (e.g., `X-Content-Type-Options`)
  SHOULD be enabled on the API.
- Logging MUST NOT record secrets or full feed content; log levels MUST be configurable.
- The application is single-user and local; authentication is out of scope until a requirement
  introduces it, and the app MUST NOT be exposed to untrusted networks without an amendment
  addressing authentication and rate limiting.

## Development Workflow & Quality Gates

- Work proceeds through the Spec Kit flow (specify → plan → tasks → implement); specs MUST state
  which phase (MVP, Extended-MVP, post-MVP) they target.
- Every change MUST go through a pull request with at least one review (self-review with the
  checklist below is acceptable for a solo contributor).
- A PR MUST NOT be merged unless: the build has zero warnings, formatting passes, all tests pass,
  the vulnerable-package check is clean, and the local development checklist in
  `ProjectGoals.md` has been verified for UI-affecting changes.
- Reviewers MUST verify compliance with this constitution; any justified deviation MUST be recorded
  in the PR description and in the plan's complexity-tracking section.
- Commits MUST be small and focused with descriptive messages.

## Governance

This constitution supersedes other development practices for this project. Where
`StakeholderDocuments/` describes product scope and this constitution describes engineering rules,
both apply; conflicts MUST be resolved by amending one of them explicitly.

- **Amendments**: Proposed via pull request that updates this file, includes a Sync Impact Report,
  states the rationale, and is approved by the project owner. Amendments that change behavior of
  existing code MUST include a migration plan.
- **Versioning**: Semantic versioning. MAJOR for backward-incompatible principle removals or
  redefinitions; MINOR for added principles/sections or materially expanded guidance; PATCH for
  clarifications and wording fixes.
- **Compliance review**: Every PR review and every `/speckit-plan` Constitution Check MUST verify
  compliance. Constitution adherence is reviewed at the end of each phase (MVP, Extended-MVP,
  post-MVP).

**Version**: 1.0.0 | **Ratified**: 2026-10-24 | **Last Amended**: 2026-10-24
