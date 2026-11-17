using Microsoft.AspNetCore.Mvc;

namespace Aja.Api.Shared.Errors;

public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AjaProblemException ex)
        {
            await WriteProblemAsync(context, ex.StatusCode, ex.Title, ex.Message, ex.Code);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha não tratada ao processar {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, 500, "Erro interno", "Não foi possível concluir a operação.", "erro_interno");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string title,
        string detail,
        string code)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
