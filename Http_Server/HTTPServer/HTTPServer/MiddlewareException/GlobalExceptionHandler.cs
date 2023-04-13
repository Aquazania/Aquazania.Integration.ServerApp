using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace HTTPServer.MiddlewareException
{
    public class GlobalExceptionHandlerMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(ILogger<GlobalExceptionHandlerMiddleware> logger) => _logger = logger;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);

                await HandleExceptionAsync(context, exception).ConfigureAwait(false);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var details = new ProblemDetails()
            {
                Title = "Internal server error",
                Status = (int)HttpStatusCode.InternalServerError,
                Type = exception.GetType().FullName,
                Detail = exception.Message,
                Instance = context.Request.Path,
            };

            var exceptionResult = JsonConvert.SerializeObject(details);

            return context.Response.WriteAsync(exceptionResult);
        }
    }
}
