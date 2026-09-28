namespace EnterpriseBIDashboard.Models;

public sealed class ConnectionStatusModel
{
    public bool IsConnected { get; init; }
    public string ServerName { get; init; } = "localhost";
    public string CubeName { get; init; } = "EnterpriseCube";
    public string Message { get; init; } = string.Empty;
}
