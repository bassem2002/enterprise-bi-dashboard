namespace EnterpriseBIDashboard.Models;

public sealed class ChartDataPoint
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public decimal? SecondaryValue { get; init; }
}
