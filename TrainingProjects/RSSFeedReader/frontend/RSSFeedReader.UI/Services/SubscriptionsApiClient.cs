using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.UI.Services;

public sealed class SubscriptionsApiClient : ISubscriptionsApiClient
{
    private const string AddFailedMessage = "Could not add the subscription. Please try again.";

    private readonly HttpClient _http;

    public SubscriptionsApiClient(HttpClient http) => _http = http;

    public async Task<AddSubscriptionOutcome> AddAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync("subscriptions", new AddSubscriptionRequest(url), ct);
            if (response.IsSuccessStatusCode)
            {
                return new AddSubscriptionOutcome(true, null);
            }

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                var message = await ReadProblemMessageAsync(response, ct);
                if (message is not null)
                {
                    return new AddSubscriptionOutcome(false, message);
                }
            }
        }
        catch (Exception ex) when (IsRecoverable(ex, ct))
        {
        }

        return new AddSubscriptionOutcome(false, AddFailedMessage);
    }

    public async Task<IReadOnlyList<SubscriptionDto>?> ListAsync(CancellationToken ct = default)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<SubscriptionDto>>("subscriptions", ct);
        }
        catch (Exception ex) when (IsRecoverable(ex, ct))
        {
            return null;
        }
    }

    // Timeouts surface as OperationCanceledException; a caller-requested cancellation must still propagate.
    private static bool IsRecoverable(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException or JsonException or NotSupportedException
        || (ex is OperationCanceledException && !ct.IsCancellationRequested);

    private static async Task<string?> ReadProblemMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return json.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
        }

        if (json.RootElement.TryGetProperty("errors", out var errors)
            && errors.TryGetProperty("url", out var urlErrors)
            && urlErrors.ValueKind == JsonValueKind.Array
            && urlErrors.GetArrayLength() > 0)
        {
            return urlErrors[0].GetString();
        }

        return null;
    }
}
