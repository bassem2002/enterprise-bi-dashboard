namespace EnterpriseBIDashboard.Models;

public sealed class CalculatedKpiModel
{
    public decimal? MargeBrute { get; init; }
    public decimal? QuantiteNonLivree { get; init; }
    public decimal? TauxAchatsVentes { get; init; }
    public decimal? TauxLivraison { get; init; }
    public bool IsAvailable { get; init; } = true;
    public string? Message { get; init; }
}
