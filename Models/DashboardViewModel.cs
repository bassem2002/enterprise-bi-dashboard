namespace EnterpriseBIDashboard.Models;

public sealed class DashboardViewModel
{
    public string ServerName { get; init; } = "localhost";
    public string CubeName { get; init; } = "EnterpriseCube";
    public string DataWarehouseName { get; init; } = "EnterpriseDW";
}
