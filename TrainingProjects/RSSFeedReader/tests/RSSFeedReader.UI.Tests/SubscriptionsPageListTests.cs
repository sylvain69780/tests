using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RSSFeedReader.UI.Pages;
using RSSFeedReader.UI.Services;

namespace RSSFeedReader.UI.Tests;

public class SubscriptionsPageListTests : BunitContext
{
    private readonly FakeSubscriptionsApiClient _api = new();

    public SubscriptionsPageListTests() => Services.AddSingleton<ISubscriptionsApiClient>(_api);

    [Fact]
    public void Page_WithNoSubscriptions_ShowsEmptyState()
    {
        var cut = Render<Subscriptions>();

        cut.WaitForAssertion(() => Assert.Contains("No subscriptions yet. Add a feed address above.", cut.Markup));
        Assert.Empty(cut.FindAll("li"));
    }

    [Fact]
    public void Page_ListsSubscriptionsInOrder()
    {
        _api.Seed("https://b.example/feed", "https://a.example/feed");

        var cut = Render<Subscriptions>();

        cut.WaitForAssertion(() =>
            Assert.Equal(["https://b.example/feed", "https://a.example/feed"], cut.FindAll("li").Select(li => li.TextContent.Trim())));
    }

    [Fact]
    public void Submit_Success_ShowsNewEntryWithoutReload()
    {
        var cut = Render<Subscriptions>();

        cut.Find("input").Input("https://example.com/feed");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal(["https://example.com/feed"], cut.FindAll("li").Select(li => li.TextContent.Trim())));
        Assert.DoesNotContain("No subscriptions yet", cut.Markup);
    }

    [Fact]
    public void Page_RendersAddressesAsEncodedText()
    {
        _api.Seed("<b>x</b>");

        var cut = Render<Subscriptions>();

        cut.WaitForAssertion(() => Assert.Contains("&lt;b&gt;x&lt;/b&gt;", cut.Markup));
        Assert.Empty(cut.FindAll("li b"));
    }

    [Fact]
    public void Page_WhenListCannotBeLoaded_ShowsMessageInAlert()
    {
        _api.ListFails = true;

        var cut = Render<Subscriptions>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Could not load your subscriptions. Please try again.", cut.Find("[role='alert']").TextContent));
    }
}
