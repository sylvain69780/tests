using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.UI.Services;

public record AddSubscriptionOutcome(bool Succeeded, string? ErrorMessage);

public interface ISubscriptionsApiClient
{
    Task<AddSubscriptionOutcome> AddAsync(string url, CancellationToken ct = default);

    // Returns null when the list could not be loaded.
    Task<IReadOnlyList<SubscriptionDto>?> ListAsync(CancellationToken ct = default);
}
