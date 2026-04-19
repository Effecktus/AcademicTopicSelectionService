using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace AcademicTopicSelectionService.API.Exceptions;

/// <summary>
/// Единый ответ для необработанных исключений (RFC 7807 Problem Details).
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Необработанное исключение при обработке запроса");

        var detail = environment.IsDevelopment()
            ? $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}"
            : "Произошла внутренняя ошибка. Повторите запрос позже.";

        await Results.Problem(
                title: "Internal Server Error",
                detail: detail,
                statusCode: StatusCodes.Status500InternalServerError,
                instance: httpContext.Request.Path.Value)
            .ExecuteAsync(httpContext);

        return true;
    }
}
