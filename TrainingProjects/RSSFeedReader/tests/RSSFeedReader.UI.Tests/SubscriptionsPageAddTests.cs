using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RSSFeedReader.UI.Pages;
using RSSFeedReader.UI.Services;

namespace RSSFeedReader.UI.Tests;

public class SubscriptionsPageAddTests : BunitContext
{
    private readonly FakeSubscriptionsApiClient _api = new();

    public SubscriptionsPageAddTests() => Services.AddSingleton<ISubscriptionsApiClient>(_api);

    [Fact]
    public void Page_HasLabelledFeedAddressInput()
    {
        var cut = Render<Subscriptions>();

        var input = cut.Find("input");
        var label = cut.Find($"label[for='{input.Id}']");
        Assert.Equal("Feed address", label.TextContent.Trim());
    }

    [Fact]
    public void Submit_CallsApiWithTypedTextAndClearsInputOnSuccess()
    {
        var cut = Render<Subscriptions>();

        cut.Find("input").Input("https://example.com/feed");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("", cut.Find("input").GetAttribute("value") ?? ""));
        Assert.Equal(["https://example.com/feed"], _api.AddCalls);
    }

    [Fact]
    public void Submit_OnFailure_KeepsTextAndShowsMessageInAlert()
    {
        _api.NextAddOutcome = new(false, "A feed address is required.");
        var cut = Render<Subscriptions>();

        cut.Find("input").Input("bad");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("A feed address is required.", cut.Find("[role='alert']").TextContent));
        Assert.Equal("bad", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Input_HasMaxLengthOf2048()
    {
        var cut = Render<Subscriptions>();

        Assert.Equal("2048", cut.Find("input").GetAttribute("maxlength"));
    }

    [Fact]
    public void Submit_DuplicateMessage_IsShownAndClearedOnNextSubmit()
    {
        _api.NextAddOutcome = new(false, "You are already subscribed to this feed.");
        var cut = Render<Subscriptions>();
        cut.Find("input").Input("https://example.com/feed");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("already subscribed", cut.Find("[role='alert']").TextContent));

        _api.NextAddOutcome = new(true, null);
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.DoesNotContain("already subscribed", cut.Find("[role='alert']").TextContent));
    }

    [Fact]
    public void AddButton_IsDisabledWhileRequestIsPending()
    {
        _api.PendingAdd = new TaskCompletionSource<AddSubscriptionOutcome>();
        var cut = Render<Subscriptions>();

        cut.Find("input").Input("https://example.com/feed");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.True(cut.Find("button").HasAttribute("disabled")));

        _api.PendingAdd.SetResult(new AddSubscriptionOutcome(true, null));
        cut.WaitForAssertion(() => Assert.False(cut.Find("button").HasAttribute("disabled")));
    }
}
