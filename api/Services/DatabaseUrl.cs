using Npgsql;

namespace Testify.Api.Services;

public static class DatabaseUrl
{
    // Looked up by name instead of Enum.Parse, so a value like "1" or "verify_full" is rejected, not guessed.
    private static readonly Dictionary<string, SslMode> SslModes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["disable"] = SslMode.Disable,
        ["allow"] = SslMode.Allow,
        ["prefer"] = SslMode.Prefer,
        ["require"] = SslMode.Require,
        ["verify-ca"] = SslMode.VerifyCA,
        ["verify-full"] = SslMode.VerifyFull
    };

    private static readonly Dictionary<string, ChannelBinding> ChannelBindings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["disable"] = ChannelBinding.Disable,
        ["prefer"] = ChannelBinding.Prefer,
        ["require"] = ChannelBinding.Require
    };

    // Turns a postgres:// URL, which is how Neon and Render hand a service its database, into the
    // "Host=...;Username=..." form that Npgsql expects. Anything else is rejected.
    // sslmode and channel_binding in the query string are kept, so a URL that says "require" is enforced.
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

        var query = ReadQuery(uri.Query);

        if (query.TryGetValue("sslmode", out var sslMode))
        {
            if (!SslModes.TryGetValue(sslMode, out var parsedSslMode))
                throw new FormatException("DATABASE_URL has an unknown sslmode");
            builder.SslMode = parsedSslMode;
        }

        if (query.TryGetValue("channel_binding", out var channelBinding))
        {
            if (!ChannelBindings.TryGetValue(channelBinding, out var parsedBinding))
                throw new FormatException("DATABASE_URL has an unknown channel_binding");
            builder.ChannelBinding = parsedBinding;
        }

        return builder.ConnectionString;
    }

    // "?sslmode=require&channel_binding=require" becomes a case-insensitive name/value lookup.
    private static Dictionary<string, string> ReadQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            values[Uri.UnescapeDataString(parts[0])] = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        }
        return values;
    }
}