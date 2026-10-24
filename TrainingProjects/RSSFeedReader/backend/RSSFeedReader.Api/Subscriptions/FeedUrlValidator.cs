using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Subscriptions;

public record FeedUrlValidationResult(bool IsValid, string? Error, string Url, string Key)
{
    public static FeedUrlValidationResult Invalid(string error) => new(false, error, string.Empty, string.Empty);

    public static FeedUrlValidationResult Valid(string url, string key) => new(true, null, url, key);
}

public static class FeedUrlValidator
{
    public const string RequiredMessage = "A feed address is required.";
    public const string TooLongMessage = "The feed address is too long (maximum 2,048 characters).";
    public const string NotWebAddressMessage = "Enter a valid http or https web address.";

    public static FeedUrlValidationResult Validate(string? raw)
    {
        var url = raw?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            return FeedUrlValidationResult.Invalid(RequiredMessage);
        }

        if (url.Length > SubscriptionLimits.MaxUrlLength)
        {
            return FeedUrlValidationResult.Invalid(TooLongMessage);
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrEmpty(uri.Host))
        {
            return FeedUrlValidationResult.Invalid(NotWebAddressMessage);
        }

        return FeedUrlValidationResult.Valid(url, BuildKey(uri));
    }

    // Scheme and host are already lowercased by Uri; path and query stay case-sensitive.
    private static string BuildKey(Uri uri)
    {
        var userInfo = uri.UserInfo.Length > 0 ? uri.UserInfo + "@" : string.Empty;
        var port = uri.IsDefaultPort ? string.Empty : ":" + uri.Port;
        var path = uri.AbsolutePath.EndsWith('/') ? uri.AbsolutePath[..^1] : uri.AbsolutePath;
        var query = uri.Query.Length > 1 ? uri.Query : string.Empty;

        return $"{uri.Scheme}://{userInfo}{uri.Host}{port}{path}{query}";
    }
}
