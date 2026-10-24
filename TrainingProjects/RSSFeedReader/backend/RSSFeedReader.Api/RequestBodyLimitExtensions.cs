using Microsoft.AspNetCore.Http.Features;

namespace RSSFeedReader.Api;

public static class RequestBodyLimitExtensions
{
    public const int MaxRequestBodyBytes = 4096;

    // Rejects oversized bodies up front via Content-Length, and lets the server enforce the cap for chunked bodies.
    public static IApplicationBuilder UseRequestBodyLimit(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength > MaxRequestBodyBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context,
                    ProblemDetails =
                    {
                        Status = StatusCodes.Status413PayloadTooLarge,
                        Title = "Request body too large",
                    },
                });
                return;
            }

            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature is { IsReadOnly: false })
            {
                feature.MaxRequestBodySize = MaxRequestBodyBytes;
            }

            await next();
        });
}
