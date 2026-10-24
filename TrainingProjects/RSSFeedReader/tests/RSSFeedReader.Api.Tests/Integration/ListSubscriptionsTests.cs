using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Tests.Integration;

public sealed class ListSubscriptionsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Get_FreshStart_ReturnsEmptyArray()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/subscriptions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<SubscriptionDto>>();
        Assert.Empty(list!);
    }

    [Fact]
    public async Task Get_AfterPosts_ReturnsSubscriptionsInOrder()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("https://b.example/feed"));
        await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("https://a.example/feed"));

        var list = await client.GetFromJsonAsync<List<SubscriptionDto>>("/api/subscriptions");

        Assert.Equal(["https://b.example/feed", "https://a.example/feed"], list!.Select(s => s.Url).ToArray());
    }

    [Fact]
    public async Task Get_After100Posts_ReturnsAllInOrder()
    {
        var client = _factory.CreateClient();
        var expected = Enumerable.Range(0, 100).Select(i => $"https://example.com/feed/{i}").ToArray();
        foreach (var url in expected)
        {
            await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest(url));
        }

        var list = await client.GetFromJsonAsync<List<SubscriptionDto>>("/api/subscriptions");

        Assert.Equal(expected, list!.Select(s => s.Url).ToArray());
    }
}
