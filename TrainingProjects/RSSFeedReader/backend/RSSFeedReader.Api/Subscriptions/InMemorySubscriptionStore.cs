using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Subscriptions;

public sealed class InMemorySubscriptionStore : ISubscriptionStore
{
    private readonly object _lock = new();
    private readonly List<SubscriptionDto> _items = [];
    private readonly HashSet<string> _keys = [];

    public AddResult TryAdd(string url, string key)
    {
        lock (_lock)
        {
            if (!_keys.Add(key))
            {
                return AddResult.Duplicate;
            }

            _items.Add(new SubscriptionDto(url));
            return AddResult.Added;
        }
    }

    public IReadOnlyList<SubscriptionDto> GetAll()
    {
        lock (_lock)
        {
            return _items.ToList();
        }
    }
}
