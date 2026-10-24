# UI Contract: Subscriptions Page

Single page served at `/` (the only route using `@page "/"`), titled "Subscriptions".

## Elements

| Element | Behavior |
|---------|----------|
| Text input, visible label "Feed address" | `maxlength` 2048; keeps its text after a rejected add; cleared after a successful add (FR-009). |
| "Add" button | Submits the input. Also triggered by pressing Enter in the input. Disabled while a request is in flight. |
| Message area (`role="alert"`) | Shows the single most recent error; cleared on the next submit or successful add. |
| Subscription list | One entry per subscription, in insertion order, rendered as plain text (FR-010). Shown without a page reload. |
| Empty-state message | Shown instead of the list when there are no subscriptions (FR-004). |

## Messages (user-visible text)

| Situation | Text | Source |
|-----------|------|--------|
| Empty input | A feed address is required. | API 400 (also checked client-side) |
| Too long | The feed address is too long (maximum 2,048 characters). | API 400 |
| Not a web address | Enter a valid http or https web address. | API 400 |
| Duplicate | You are already subscribed to this feed. | API 409 |
| Save failed (network or 5xx) | Could not add the subscription. Please try again. | UI fallback (FR-011) |
| Load failed (network or 5xx) | Could not load your subscriptions. Please try again. | UI fallback (FR-011) |
| No subscriptions | No subscriptions yet. Add a feed address above. | UI |

The UI never shows raw exception text, status codes, or stack traces.

## Configuration

The API base address is read from `wwwroot/appsettings.json` key `ApiBaseUrl` (default `http://localhost:5151/api/`). It is never hard-coded in components.

## Accessibility

Semantic form with a labelled input, keyboard-operable controls, and a message area announced to assistive technology.
