using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Subscriptions;

public enum AddResult
{
    Added,
    Duplicate,
}

public interface ISubscriptionStore
{
    AddResult TryAdd(string url, string key);

    IReadOnlyList<SubscriptionDto> GetAll();
}
