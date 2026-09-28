namespace EnterpriseBIDashboard.Models;

public sealed record SsasQueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows);
