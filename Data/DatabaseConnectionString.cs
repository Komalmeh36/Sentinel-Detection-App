
using Npgsql;

namespace Sentinel.Web.Data
{
    public static class DatabaseConnectionString
    {
        public static string ConvertToNpgsql(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is missing.");
            }

            // Already in Npgsql format
            if (connectionString.StartsWith(
                "Host=", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            // Convert Neon's PostgreSQL URL
            if (!Uri.TryCreate(
                connectionString, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "postgresql" &&
                 uri.Scheme != "postgres"))
            {
                throw new InvalidOperationException(
                    "Invalid PostgreSQL connection URL.");
            }

            var credentials = uri.UserInfo.Split(':', 2);

            if (credentials.Length != 2)
            {
                throw new InvalidOperationException(
                    "Database credentials are incomplete.");
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(
                    uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(credentials[0]),
                Password = Uri.UnescapeDataString(credentials[1]),
                SslMode = SslMode.Require,
                ChannelBinding = ChannelBinding.Require
            };

            return builder.ConnectionString;
        }
    }
}
