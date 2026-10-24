using RSSFeedReader.Api.Subscriptions;

namespace RSSFeedReader.Api.Tests.Unit;

public class InMemorySubscriptionStoreTests
{
    private readonly InMemorySubscriptionStore _store = new();

    [Fact]
    public void GetAll_EmptyStore_ReturnsEmptyList()
    {
        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public void TryAdd_KeepsSubscriptionsInInsertionOrder()
    {
        _store.TryAdd("https://b.example/feed", "https://b.example/feed");
        _store.TryAdd("https://a.example/feed", "https://a.example/feed");

        var urls = _store.GetAll().Select(s => s.Url).ToArray();

        Assert.Equal(["https://b.example/feed", "https://a.example/feed"], urls);
    }

    [Fact]
    public void GetAll_ReturnsSnapshotUnaffectedByLaterAdds()
    {
        _store.TryAdd("https://a.example/feed", "https://a.example/feed");
        var snapshot = _store.GetAll();

        _store.TryAdd("https://b.example/feed", "https://b.example/feed");

        Assert.Single(snapshot);
    }

    [Fact]
    public async Task TryAdd_ConcurrentAddsOfDistinctAddresses_LosesNone()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => _store.TryAdd($"https://example.com/{i}", $"https://example.com/{i}")));

        await Task.WhenAll(tasks);

        Assert.Equal(100, _store.GetAll().Count);
    }

    [Fact]
    public void TryAdd_SameKeyTwice_ReturnsDuplicateAndKeepsList()
    {
        Assert.Equal(AddResult.Added, _store.TryAdd("https://example.com/feed", "key"));

        Assert.Equal(AddResult.Duplicate, _store.TryAdd("https://example.com/feed/", "key"));

        Assert.Equal(["https://example.com/feed"], _store.GetAll().Select(s => s.Url).ToArray());
    }

    [Fact]
    public async Task TryAdd_ConcurrentAddsOfSameKey_StoresExactlyOne()
    {
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() => _store.TryAdd("https://example.com/feed", "key")));

        var results = await Task.WhenAll(tasks);

        Assert.Single(_store.GetAll());
        Assert.Equal(1, results.Count(r => r == AddResult.Added));
    }
}
