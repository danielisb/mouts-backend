using Ambev.DeveloperEvaluation.Domain.Exceptions;
using FluentValidation;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

public class SalesExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SalesExceptionMiddleware> _logger;

    public SalesExceptionMiddleware(
        RequestDelegate next,
        ILogger<SalesExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/sales"))
        {
            await _next(context);
            return;
        }

        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var (status, type, error, detail) = exception switch
            {
                ValidationException validation => (
                    400,
                    "ValidationError",
                    "Invalid input data",
                    string.Join(" ", validation.Errors.Select(item => item.ErrorMessage))),

                DomainException domain => (
                    400,
                    "ValidationError",
                    "Invalid sale",
                    domain.Message),

                SaleNotFoundException missing => (
                    404,
                    "ResourceNotFound",
                    "Sale not found",
                    missing.Message),

                SaleConflictException conflict => (
                    409,
                    "Conflict",
                    "Sale operation rejected",
                    conflict.Message),

                _ => (
                    500,
                    "InternalServerError",
                    "Unexpected error",
                    "An unexpected error occurred while processing the request.")
            };

            if (status == 500)
                _logger.LogError(exception, "Unexpected error in sales API.");

            context.Response.StatusCode = status;

            await context.Response.WriteAsJsonAsync(new
            {
                type,
                error,
                detail
            });
        }
    }
}