using System.ComponentModel.DataAnnotations;

namespace Aja.Api.Modules.Demandas.Infrastructure;

public sealed class PilotoOptions
{
    public const string SectionName = "Piloto";
    [Required, StringLength(100, MinimumLength = 2)] public string Cidade { get; init; } = "São Paulo";
    [Required, StringLength(100, MinimumLength = 2)] public string Bairro { get; init; } = "Doroteia";
}
