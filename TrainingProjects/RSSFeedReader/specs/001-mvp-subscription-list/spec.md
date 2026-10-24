# Feature Specification: MVP Feed Subscription List

**Feature Branch**: `001-mvp-subscription-list`

**Created**: 2026-10-24

**Status**: Draft

**Input**: User description: "MVP RSS reader: a simple RSS/Atom feed reader that demonstrates the most basic capability (add subscriptions) without the complexity of a production-ready application."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add a feed subscription by URL (Priority: P1)

A user opens the app, pastes the web address of an RSS/Atom feed into an input field, and confirms. The feed is added to their list of subscriptions.

**Why this priority**: Adding a subscription is the core capability the MVP exists to demonstrate. Without it there is nothing to list.

**Independent Test**: Open the app, paste a feed address, and confirm. The address is accepted and the user receives confirmation that it was added.

**Acceptance Scenarios**:

1. **Given** the app is open, **When** the user enters `https://devblogs.microsoft.com/dotnet/feed/` and confirms, **Then** the subscription is added and the input field is cleared ready for another entry.
2. **Given** the user has already added one subscription, **When** they add a different feed address, **Then** both subscriptions are kept.
3. **Given** the user leaves the input empty or enters only whitespace, **When** they confirm, **Then** nothing is added and a clear message explains that a feed address is required.

---

### User Story 2 - See the list of subscriptions (Priority: P1)

A user sees all of their current subscriptions in a simple list, which updates immediately whenever a subscription is added.

**Why this priority**: Seeing the result of adding a subscription completes the MVP: "add a subscription" and "see the updated list" are the two criteria for "MVP working".

**Independent Test**: Open the app with no subscriptions and see an empty-state message. Add a subscription and see it appear in the list without reloading the page.

**Acceptance Scenarios**:

1. **Given** the user has no subscriptions, **When** the app opens, **Then** the list area shows a friendly message that no subscriptions exist yet.
2. **Given** the user has subscriptions, **When** the app opens or they add another, **Then** every subscription is shown, in the order added, with the new one appearing immediately.
3. **Given** the user adds a subscription, **When** the list updates, **Then** each entry shows the feed address as entered.

---

### User Story 3 - Receive clear feedback on unusable input (Priority: P2)

A user who enters something that cannot be a feed address (for example, plain text, an address using an unsupported scheme, or an excessively long value) is told what is wrong and can correct it.

**Why this priority**: Protects the app and the user from unusable or unsafe entries while keeping the MVP small; it does not block the core add-and-list flow.

**Independent Test**: Enter `not a url` and `javascript:alert(1)` in turn. Each is rejected with a readable message and the list is unchanged.

**Acceptance Scenarios**:

1. **Given** the user enters text that is not a complete web address, **When** they confirm, **Then** it is not added and the user sees a message asking for a valid `http` or `https` address.
2. **Given** the user enters an address using a non-web scheme (such as `file:` or `javascript:`), **When** they confirm, **Then** it is not added and the user sees the same message.
3. **Given** the user enters an address already in the list, **When** they confirm, **Then** it is not added a second time and the user is told they are already subscribed to it.

---

### Edge Cases

- Leading or trailing spaces around a pasted address are removed before the address is checked and stored.
- An address longer than the maximum allowed length (2,048 characters) is rejected with a readable message.
- Two addresses that differ only in letter case of the host name, or by a trailing slash, count as the same subscription.
- If the app is closed and reopened, all subscriptions are gone (expected MVP behavior), and the empty-state message is shown.
- If the app cannot save the subscription (for example, the service is unavailable), the user sees a readable failure message and the list is unchanged.
- The app does not check that an address points to a real or reachable feed; any well-formed web address is accepted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to add a feed subscription by entering or pasting a feed address.
- **FR-002**: The app MUST display all current subscriptions as a simple list in the order they were added.
- **FR-003**: The list MUST update immediately after a successful add, without the user reloading the page.
- **FR-004**: The app MUST show a friendly empty-state message when there are no subscriptions.
- **FR-005**: The app MUST trim surrounding whitespace from entered addresses before validating and storing them.
- **FR-006**: The app MUST reject empty entries, and MUST reject entries that are not complete, absolute `http` or `https` addresses or that exceed 2,048 characters, each with a clear, user-readable message.
- **FR-007**: The app MUST NOT add a subscription that duplicates an existing one (compared after trimming, ignoring host-name letter case and a trailing slash) and MUST tell the user it already exists.
- **FR-008**: These checks MUST be enforced by the system that stores subscriptions, not only in the user interface.
- **FR-009**: The app MUST clear the input field after a successful add and keep the entered text after a rejected add so the user can correct it.
- **FR-010**: Feed addresses MUST be displayed as plain text and MUST NOT be interpreted as markup or script.
- **FR-011**: The app MUST show a readable message, with no technical details, if a subscription cannot be saved or the list cannot be loaded.
- **FR-012**: The app MUST NOT fetch, parse, or display feed content in this feature, and MUST NOT verify that an address is a reachable or valid feed.
- **FR-013**: Subscriptions MAY be kept only for the current session; they are not required to survive closing the app.

### Key Entities

- **Subscription**: A feed the user has chosen to follow. Key attribute: the feed address as entered (after whitespace trimming). Subscriptions are kept as an ordered collection for one user.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time user can add a subscription and see it in the list within 30 seconds of opening the app, with no instructions.
- **SC-002**: After a successful add, the new subscription is visible in the list within 1 second.
- **SC-003**: 100% of entries that are empty, malformed, use a non-`http`/`https` scheme, exceed the length limit, or duplicate an existing subscription are rejected with a message, and the list is unchanged.
- **SC-004**: A user can add at least 100 subscriptions in one session and every one stays visible in the order added.
- **SC-005**: In a demonstration, both "MVP working" criteria are met: a feed address can be added by pasting it, and the UI shows the updated list.

## Assumptions

- The app has a single user running it locally; there are no accounts, sign-in, or sharing.
- The app works on Windows, macOS, and Linux and is used in a current desktop or mobile web browser.
- Subscriptions are held in memory only, so a restart clears them. Saving subscriptions between sessions, removing subscriptions, fetching feed content, and showing feed items are deferred to the Extended-MVP and later phases.
- "No validation of feed addresses" in the project goals means no check that the address is a reachable or valid feed. Basic safety checks on the address (web scheme, length, non-empty, no duplicates) still apply, per the project constitution.
- Duplicate subscriptions are rejected because a list of identical entries gives no value. This is a small addition to the stakeholder documents and can be removed if unwanted.
- The interface is simple and functional; visual polish is not a goal.
