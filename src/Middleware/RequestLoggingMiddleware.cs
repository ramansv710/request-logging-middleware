using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RequestLogging.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log basic request info
            _logger.LogInformation("Incoming Request: {Method} {Path}", context.Request.Method, context.Request.Path);

            // Log headers (can be filtered or masked)
            foreach (var header in context.Request.Headers)
            {
                _logger.LogDebug("Header: {Key} = {Value}", header.Key, header.Value);
            }

            // Swap response stream to capture response body
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            stopwatch.Stop();

            // Read and log response body (optional, use with caution)
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation("Response Status: {StatusCode}", context.Response.StatusCode);
            _logger.LogInformation("Execution Time: {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
            _logger.LogDebug("Response Body: {Body}", responseText);

            // Restore original response stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}
