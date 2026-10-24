using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RSSFeedReader.Api.Subscriptions;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Tests.Integration;

public sealed class AddSubscriptionTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Post_ValidUrlWithSpaces_Returns201WithTrimmedUrl()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("  https://example.com/feed  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        Assert.Equal("https://example.com/feed", body?.Url);
    }

    [Fact]
    public async Task Post_TwoDifferentUrls_BothAreStored()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("https://a.example/feed"));
        await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("https://b.example/feed"));

        var store = _factory.Services.GetRequiredService<ISubscriptionStore>();
        Assert.Equal(["https://a.example/feed", "https://b.example/feed"], store.GetAll().Select(s => s.Url).ToArray());
    }

    [Fact]
    public async Task Post_EmptyUrl_Returns400ProblemWithMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("A feed address is required.", json.RootElement.GetProperty("errors").GetProperty("url")[0].GetString());
        Assert.Empty(_factory.Services.GetRequiredService<ISubscriptionStore>().GetAll());
    }

    [Theory]
    [InlineData("not a url", "Enter a valid http or https web address.")]
    [InlineData("javascript:alert(1)", "Enter a valid http or https web address.")]
    [InlineData("file:///c:/secret.txt", "Enter a valid http or https web address.")]
    public async Task Post_NonWebAddress_Returns400AndLeavesListUnchanged(string url, string expectedMessage)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest(url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedMessage, json.RootElement.GetProperty("errors").GetProperty("url")[0].GetString());
        Assert.Empty(_factory.Services.GetRequiredService<ISubscriptionStore>().GetAll());
    }

    [Fact]
    public async Task Post_TooLongAddress_Returns400AndLeavesListUnchanged()
    {
        var client = _factory.CreateClient();
        var url = "https://example.com/" + new string('a', SubscriptionLimits.MaxUrlLength);

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest(url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "The feed address is too long (maximum 2,048 characters).",
            json.RootElement.GetProperty("errors").GetProperty("url")[0].GetString());
        Assert.Empty(_factory.Services.GetRequiredService<ISubscriptionStore>().GetAll());
    }

    [Fact]
    public async Task Post_DuplicateAfterNormalization_Returns409Problem()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("https://devblogs.microsoft.com/dotnet/feed/"));

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest("HTTPS://DevBlogs.Microsoft.com/dotnet/feed"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("You are already subscribed to this feed.", json.RootElement.GetProperty("detail").GetString());
        Assert.Single(_factory.Services.GetRequiredService<ISubscriptionStore>().GetAll());
    }

    [Fact]
    public async Task Post_BodyLargerThan4KB_Returns413()
    {
        var client = _factory.CreateClient();
        var url = "https://example.com/" + new string('a', 5000);

        var response = await client.PostAsJsonAsync("/api/subscriptions", new AddSubscriptionRequest(url));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(_factory.Services.GetRequiredService<ISubscriptionStore>().GetAll());
    }

    [Fact]
    public async Task Post_MalformedJson_Returns400()
    {
        var client = _factory.CreateClient();
        using var content = new StringContent("{\"url\":", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/subscriptions", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingBody_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/subscriptions", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
