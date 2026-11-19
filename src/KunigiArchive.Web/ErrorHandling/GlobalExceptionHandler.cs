using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Application.Users;
using KunigiArchive.Domain.Common;
using KunigiArchive.Web.Extensions;
using Microsoft.AspNetCore.Diagnostics;

namespace KunigiArchive.Web.ErrorHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.IsApiRequest())
        {
            return false;
        }

        (int statusCode, string title) = exception switch
        {
            DomainException or InvalidRequestException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            EmailDeliveryException => (StatusCodes.Status502BadGateway, "Email not sent"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }
}