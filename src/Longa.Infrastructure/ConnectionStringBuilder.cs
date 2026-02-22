namespace Longa.Infrastructure;

/// <summary>
/// Builds Npgsql connection strings from various sources.
/// Converts postgresql:// URLs to Npgsql's key=value format to avoid
/// NpgsqlConnectionStringBuilder URL parsing issues (e.g. sslmode vs SSL Mode,
/// unknown libpq params like channel_binding, shell truncation of & in env vars).
/// </summary>
public static class ConnectionStringBuilder
{
    private const string PostgresScheme = "postgresql://";
    private const string PostgresSchemeAlt = "postgres://";

    /// <summary>
    /// Normalizes a connection string for Npgsql. If it's a postgresql:// URL,
    /// converts to Npgsql's key=value format. Otherwise returns as-is.
    /// </summary>
    public static string NormalizeForNpgsql(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Connection string cannot be empty.", nameof(raw));

        var trimmed = raw.Trim();
        if (!IsPostgresUrl(trimmed))
            return trimmed;

        return ConvertUrlToNpgsqlFormat(trimmed);
    }

    private static bool IsPostgresUrl(string s)
    {
        return s.StartsWith(PostgresScheme, StringComparison.OrdinalIgnoreCase)
            || s.StartsWith(PostgresSchemeAlt, StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertUrlToNpgsqlFormat(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri?.Scheme is not ("postgresql" or "postgres"))
        {
            throw new ArgumentException(
                $"Invalid PostgreSQL connection URL. Expected postgresql:// or postgres:// scheme. Got: {url[..Math.Min(50, url.Length)]}...",
                nameof(url));
        }

        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(database))
            database = "postgres";

        var user = "";
        var password = "";
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var colonIndex = uri.UserInfo.IndexOf(':');
            if (colonIndex >= 0)
            {
                user = Uri.UnescapeDataString(uri.UserInfo[..colonIndex]);
                password = Uri.UnescapeDataString(uri.UserInfo[(colonIndex + 1)..]);
            }
            else
            {
                user = Uri.UnescapeDataString(uri.UserInfo);
            }
        }

        var parts = new List<string>
        {
            $"Host={host}",
            $"Port={port}",
            $"Database={database}"
        };

        if (!string.IsNullOrEmpty(user))
            parts.Add($"Username={EscapeValue(user)}");
        if (!string.IsNullOrEmpty(password))
            parts.Add($"Password={EscapeValue(password)}");

        parts.Add("SSL Mode=Require");

        return string.Join(";", parts);
    }

    private static string EscapeValue(string value)
    {
        if (value.IndexOfAny(new[] { ';', '=', '\'', '"', ' ' }) >= 0)
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
