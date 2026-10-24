using RSSFeedReader.Api.Subscriptions;
using RSSFeedReader.Shared.Contracts;

namespace RSSFeedReader.Api.Endpoints;

public static class SubscriptionsEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions");

        group.MapGet("/", (ISubscriptionStore store) => TypedResults.Ok(store.GetAll()));

        group.MapPost("/", (AddSubscriptionRequest? request, ISubscriptionStore store) =>
        {
            var validation = FeedUrlValidator.Validate(request?.Url);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = [validation.Error!] });
            }

            if (store.TryAdd(validation.Url, validation.Key) == AddResult.Duplicate)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Already subscribed",
                    detail: "You are already subscribed to this feed.");
            }

            return Results.Created("/api/subscriptions", new SubscriptionDto(validation.Url));
        });

        return app;
    }
}
