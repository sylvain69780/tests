using RSSFeedReader.Api;
using RSSFeedReader.Api.Endpoints;
using RSSFeedReader.Api.Subscriptions;

const string CorsPolicyName = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST")
        .WithHeaders("Content-Type")));

builder.Services.AddSingleton<ISubscriptionStore, InMemorySubscriptionStore>();

var app = builder.Build();

app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    await next();
});
app.UseCors(CorsPolicyName);
app.UseRequestBodyLimit();

app.MapSubscriptionsEndpoints();

app.Run();

public partial class Program;
