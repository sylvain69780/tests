using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RSSFeedReader.Api.Subscriptions;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Tests.Integration;

public sealed class HostHardeningTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Responses_CarryNoSniffHeader()
    {
        var response = await _factory.CreateClient().GetAsync("/api/subscriptions");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Cors_AllowedOrigin_IsEchoedExactly()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/subscriptions");
        request.Headers.Add("Origin", "http://localhost:5213");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal("http://localhost:5213", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Cors_UnknownOrigin_GetsNoAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/subscriptions");
        request.Headers.Add("Origin", "http://evil.example");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task UnhandledException_Returns500ProblemWithoutInternalDetails()
    {
        using var failing = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<ISubscriptionStore, ThrowingStore>()));

        var response = await failing.CreateClient().GetAsync("/api/subscriptions");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret internal failure", body);
        Assert.DoesNotContain("ThrowingStore", body);
        Assert.DoesNotContain(" at ", body);
    }

    private sealed class ThrowingStore : ISubscriptionStore
    {
        public AddResult TryAdd(string url, string key) => throw new InvalidOperationException("secret internal failure");

        public IReadOnlyList<SubscriptionDto> GetAll() => throw new InvalidOperationException("secret internal failure");
    }
}
