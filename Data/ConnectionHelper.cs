using Microsoft.Extensions.Configuration;

namespace OnlineStoreLINQ.Data;

// Finds the SQL Server connection string.
// Order: 1) environment variable ONLINESTORE_CONNECTION  2) appsettings.json
public static class ConnectionHelper
{
    public const string EnvironmentVariableName = "ONLINESTORE_CONNECTION";
    private const string Placeholder = "YOUR_SERVER_NAME";

    public static string? Get()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        return config.GetConnectionString("OnlineStore");
    }

    // True when nobody replaced YOUR_SERVER_NAME with a real server name.
    public static bool IsPlaceholder(string? connectionString)
        => string.IsNullOrWhiteSpace(connectionString)
           || connectionString.Contains(Placeholder, StringComparison.OrdinalIgnoreCase);

    public static string Build(string serverName)
        => $"Server={serverName};Database=OnlineStoreDb;Trusted_Connection=True;TrustServerCertificate=True;";
}
