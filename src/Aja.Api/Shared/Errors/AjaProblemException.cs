namespace Aja.Api.Shared.Errors;

public sealed class AjaProblemException(
    int statusCode,
    string title,
    string detail,
    string code) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Code { get; } = code;

    public static AjaProblemException BadRequest(string detail, string code = "dados_invalidos") =>
        new(400, "Dados inválidos", detail, code);

    public static AjaProblemException Unauthorized(string detail = "Credenciais inválidas.") =>
        new(401, "Não autenticado", detail, "nao_autenticado");

    public static AjaProblemException Forbidden(string detail = "Você não tem permissão para esta operação.") =>
        new(403, "Acesso negado", detail, "acesso_negado");

    public static AjaProblemException NotFound(string detail = "Recurso não encontrado.") =>
        new(404, "Não encontrado", detail, "nao_encontrado");

    public static AjaProblemException Conflict(string detail, string code = "conflito_regra") =>
        new(409, "Conflito", detail, code);
}
