# Data Model: MVP Feed Subscription List

## Entity: Subscription

A feed the user has chosen to follow. Held in memory for the lifetime of the API process (spec FR-013). There is one implicit user; there is no user entity.

| Field | Type | Description |
|-------|------|-------------|
| `Url` | text | The feed address as entered by the user, after whitespace trimming. Displayed exactly as stored. |

Internal (not exposed through the API): a **normalized key** derived from `Url`, used only for duplicate detection.

The collection is **ordered by insertion** (oldest first). No identifier, timestamp, or title is stored in the MVP; none is needed by the spec. When persistence is added later, an identifier will be introduced with that feature.

## Validation rules

Applied server-side to every add request, in this order. The first failing rule determines the message.

| # | Rule | Spec reference | Outcome on failure |
|---|------|----------------|--------------------|
| 1 | Trim leading/trailing whitespace | FR-005 | n/a (normalization) |
| 2 | Value MUST NOT be empty | FR-006, US1-3 | 400, "A feed address is required." |
| 3 | Length MUST be at most 2,048 characters (after trimming) | FR-006, Edge Cases | 400, "The feed address is too long (maximum 2,048 characters)." |
| 4 | MUST parse as an absolute URI with scheme `http` or `https` and a non-empty host | FR-006, US3 | 400, "Enter a valid http or https web address." |
| 5 | MUST NOT duplicate an existing subscription (compared by normalized key) | FR-007 | 409, "You are already subscribed to this feed." |

Rule 5 and the append are performed as a single atomic operation inside the store.

## Normalized key

Computed from the parsed URI:

- scheme and host in lowercase
- port omitted when it is the default for the scheme
- fragment removed
- path with a single trailing `/` removed (the root path `/` becomes empty)
- query kept as is
- path and query case preserved

Examples (all of the first group are the same subscription):

- `https://Example.com/feed`, `https://example.com/feed/`, `https://example.com:443/feed#top`

Different subscriptions: `https://example.com/feed` vs `https://example.com/Feed`, or `http://` vs `https://`.

## State transitions

```text
(not subscribed) --valid, non-duplicate add--> (subscribed, appended at end of list)
(not subscribed) --invalid or duplicate add--> (unchanged; error returned)
(subscribed)     --process restart-----------> (not subscribed)   # in-memory only
```

There are no update or delete transitions in the MVP.

## Constants

| Name | Value | Used by |
|------|-------|---------|
| `MaxUrlLength` | 2048 | Validator, UI input `maxlength`, tests (defined in the shared contracts library) |
