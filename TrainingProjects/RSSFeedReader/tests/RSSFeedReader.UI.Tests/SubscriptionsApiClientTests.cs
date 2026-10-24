using System.Net;
using System.Text;
using RSSFeedReader.UI.Services;

namespace RSSFeedReader.UI.Tests;

public class SubscriptionsApiClientTests
{
    private static SubscriptionsApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://localhost:5151/api/") });

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/problem+json") };

    [Fact]
    public async Task AddAsync_Created_Succeeds()
    {
        var client = CreateClient(_ => Json(HttpStatusCode.Created, "{\"url\":\"https://example.com/feed\"}"));

        var outcome = await client.AddAsync("https://example.com/feed");

        Assert.True(outcome.Succeeded);
    }

    [Fact]
    public async Task AddAsync_BadRequest_ReturnsFirstUrlErrorMessage()
    {
        var client = CreateClient(_ => Json(HttpStatusCode.BadRequest, "{\"errors\":{\"url\":[\"Enter a valid http or https web address.\"]}}"));

        var outcome = await client.AddAsync("nope");

        Assert.False(outcome.Succeeded);
        Assert.Equal("Enter a valid http or https web address.", outcome.ErrorMessage);
    }

    [Fact]
    public async Task AddAsync_Conflict_ReturnsProblemDetail()
    {
        var client = CreateClient(_ => Json(HttpStatusCode.Conflict, "{\"title\":\"Already subscribed\",\"detail\":\"You are already subscribed to this feed.\"}"));

        var outcome = await client.AddAsync("https://example.com/feed");

        Assert.False(outcome.Succeeded);
        Assert.Equal("You are already subscribed to this feed.", outcome.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge)]
    public async Task AddAsync_OtherFailures_ReturnGenericMessage(HttpStatusCode status)
    {
        var client = CreateClient(_ => Json(status, "{\"title\":\"boom\",\"detail\":\"stack trace here\"}"));

        var outcome = await client.AddAsync("https://example.com/feed");

        Assert.Equal("Could not add the subscription. Please try again.", outcome.ErrorMessage);
    }

    [Fact]
    public async Task AddAsync_NetworkFailure_ReturnsGenericMessageWithoutExceptionText()
    {
        var client = CreateClient(_ => throw new HttpRequestException("connection refused to 10.0.0.1"));

        var outcome = await client.AddAsync("https://example.com/feed");

        Assert.False(outcome.Succeeded);
        Assert.Equal("Could not add the subscription. Please try again.", outcome.ErrorMessage);
    }

    [Fact]
    public async Task ListAsync_ServerError_ReturnsNull()
    {
        var client = CreateClient(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        Assert.Null(await client.ListAsync());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
