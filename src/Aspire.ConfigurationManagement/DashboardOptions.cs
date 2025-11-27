namespace Aspire.ConfigurationManagement;

public class DashboardOptions
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 8080;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}