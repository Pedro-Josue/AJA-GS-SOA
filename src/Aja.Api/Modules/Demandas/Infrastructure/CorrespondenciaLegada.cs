namespace Aja.Api.Modules.Demandas.Infrastructure;

// Rastreia a conversão sem manter as entidades do fluxo anterior no modelo EF de runtime.
public sealed class CorrespondenciaLegada
{
    public string OrigemTipo { get; private set; } = string.Empty;
    public string OrigemId { get; private set; } = string.Empty;
    public string DestinoTipo { get; private set; } = string.Empty;
    public string DestinoId { get; private set; } = string.Empty;
    public string Tratamento { get; private set; } = string.Empty;
}
