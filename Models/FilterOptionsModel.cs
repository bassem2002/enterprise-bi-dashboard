namespace EnterpriseBIDashboard.Models;

public sealed class FilterOptionsModel
{
    public IReadOnlyList<FilterOption> Years { get; init; } = [];
    public IReadOnlyList<FilterOption> Months { get; init; } = [];
    public IReadOnlyList<FilterOption> Products { get; init; } = [];
    public IReadOnlyList<FilterOption> Customers { get; init; } = [];
    public IReadOnlyList<FilterOption> Suppliers { get; init; } = [];
    public IReadOnlyList<FilterOption> Countries { get; init; } = [];
}
