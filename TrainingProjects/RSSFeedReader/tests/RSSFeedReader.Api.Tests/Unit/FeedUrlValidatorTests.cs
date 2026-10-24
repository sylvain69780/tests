using RSSFeedReader.Api.Subscriptions;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Tests.Unit;

public class FeedUrlValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t \n")]
    public void Validate_EmptyOrWhitespace_IsInvalid(string? input)
    {
        var result = FeedUrlValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("A feed address is required.", result.Error);
    }

    [Fact]
    public void Validate_SurroundingWhitespace_IsTrimmed()
    {
        var result = FeedUrlValidator.Validate("  https://example.com/feed  ");

        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.Equal("https://example.com/feed", result.Url);
    }

    [Fact]
    public void Validate_At2048Characters_IsValid()
    {
        var url = "https://example.com/" + new string('a', SubscriptionLimits.MaxUrlLength - "https://example.com/".Length);

        Assert.Equal(SubscriptionLimits.MaxUrlLength, url.Length);
        Assert.True(FeedUrlValidator.Validate(url).IsValid);
    }

    [Fact]
    public void Validate_At2049Characters_IsTooLong()
    {
        var url = "https://example.com/" + new string('a', SubscriptionLimits.MaxUrlLength + 1 - "https://example.com/".Length);

        var result = FeedUrlValidator.Validate(url);

        Assert.False(result.IsValid);
        Assert.Equal("The feed address is too long (maximum 2,048 characters).", result.Error);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("example.com/feed")]
    [InlineData("http://")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/x")]
    [InlineData("ftp://example.com/f")]
    [InlineData("data:text/plain,hi")]
    public void Validate_NotAnHttpOrHttpsAddress_IsInvalid(string input)
    {
        var result = FeedUrlValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Equal("Enter a valid http or https web address.", result.Error);
    }

    [Theory]
    [InlineData("https://Example.com/feed", "https://example.com/feed/")]
    [InlineData("https://example.com/feed", "https://example.com:443/feed#top")]
    [InlineData("http://example.com", "http://example.com/")]
    [InlineData("https://example.com/feed?a=1", "HTTPS://EXAMPLE.COM/feed/?a=1")]
    public void Validate_EquivalentAddresses_ShareTheSameKey(string first, string second)
    {
        Assert.Equal(FeedUrlValidator.Validate(first).Key, FeedUrlValidator.Validate(second).Key);
    }

    [Theory]
    [InlineData("https://example.com/feed", "https://example.com/Feed")]
    [InlineData("http://example.com/feed", "https://example.com/feed")]
    [InlineData("https://example.com/feed", "https://example.com:8443/feed")]
    [InlineData("https://example.com/feed?a=1", "https://example.com/feed?a=2")]
    public void Validate_DifferentAddresses_HaveDifferentKeys(string first, string second)
    {
        Assert.NotEqual(FeedUrlValidator.Validate(first).Key, FeedUrlValidator.Validate(second).Key);
    }

    [Fact]
    public void Validate_StoresAddressAsEnteredAfterTrimming()
    {
        var result = FeedUrlValidator.Validate(" HTTPS://Example.com/Feed/ ");

        Assert.Equal("HTTPS://Example.com/Feed/", result.Url);
    }
}
