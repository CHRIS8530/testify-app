using Npgsql;

namespace Testify.Api.Services;

public static class DatabaseUrl
{
    // Turns a postgres:// URL, which is how Render hands a service its database, into the
    // "Host=...;Username=..." form that Npgsql expects. Anything else is rejected.
    public static string ToConnectionString(string url)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            throw new FormatException("DATABASE_URL must be a postgres:// or postgresql:// URL");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/')
        };

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            // The user name and password are percent-encoded in a URL, so p@ss is written p%40ss.
            var parts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
                builder.Password = Uri.UnescapeDataString(parts[1]);
        }

        return builder.ConnectionString;
    }
}