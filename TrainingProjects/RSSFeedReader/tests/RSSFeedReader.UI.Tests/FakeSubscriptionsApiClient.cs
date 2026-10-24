using RSSFeedReader.Shared.Contracts;
using RSSFeedReader.UI.Services;

namespace RSSFeedReader.UI.Tests;

internal sealed class FakeSubscriptionsApiClient : ISubscriptionsApiClient
{
    private readonly List<SubscriptionDto> _items = [];

    public List<string> AddCalls { get; } = [];

    public AddSubscriptionOutcome NextAddOutcome { get; set; } = new(true, null);

    public bool ListFails { get; set; }

    public TaskCompletionSource<AddSubscriptionOutcome>? PendingAdd { get; set; }

    public void Seed(params string[] urls) => _items.AddRange(urls.Select(u => new SubscriptionDto(u)));

    public async Task<AddSubscriptionOutcome> AddAsync(string url, CancellationToken ct = default)
    {
        AddCalls.Add(url);
        var outcome = PendingAdd is null ? NextAddOutcome : await PendingAdd.Task;
        if (outcome.Succeeded)
        {
            _items.Add(new SubscriptionDto(url));
        }

        return outcome;
    }

    public Task<IReadOnlyList<SubscriptionDto>?> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SubscriptionDto>?>(ListFails ? null : _items.ToList());
}
