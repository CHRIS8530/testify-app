using Npgsql;
using Testify.Api.Services;
using Xunit;

namespace Testify.Api.Tests;

public class DatabaseUrlTests
{
    [Fact]
    public void ToConnectionString_ReadsHostPortDatabaseUserAndPassword()
    {
        var result = DatabaseUrl.ToConnectionString("postgres://appuser:secret@db.example.com:5433/appdb");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(5433, builder.Port);
        Assert.Equal("appdb", builder.Database);
        Assert.Equal("appuser", builder.Username);
        Assert.Equal("secret", builder.Password);
    }

    [Fact]
    public void ToConnectionString_UsesPort5432WhenTheUrlHasNoPort()
    {
        var result = DatabaseUrl.ToConnectionString("postgres://appuser:secret@db.example.com/appdb");

        Assert.Equal(5432, new NpgsqlConnectionStringBuilder(result).Port);
    }

    [Fact]
    public void ToConnectionString_AcceptsThePostgresqlScheme()
    {
        var result = DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com:5432/appdb");

        Assert.Equal("appdb", new NpgsqlConnectionStringBuilder(result).Database);
    }

    [Fact]
    public void ToConnectionString_DecodesSpecialCharactersInThePassword()
    {
        var result = DatabaseUrl.ToConnectionString("postgres://appuser:p%40ss%2Fword@db.example.com:5432/appdb");

        Assert.Equal("p@ss/word", new NpgsqlConnectionStringBuilder(result).Password);
    }

    [Theory]
    [InlineData("mysql://appuser:secret@db.example.com/appdb")]
    [InlineData("Host=db.example.com;Database=appdb")]
    [InlineData("")]
    public void ToConnectionString_RejectsAnythingThatIsNotAPostgresUrl(string value)
    {
        Assert.Throws<FormatException>(() => DatabaseUrl.ToConnectionString(value));
    }

    // Neon and Render hand over URLs like postgresql://user:pass@host/db?sslmode=require.
    // The query string used to be dropped, so TLS was requested by Npgsql's default but never enforced.

    [Fact]
    public void ToConnectionString_ReadsSslModeFromTheQueryString()
    {
        var result = DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb?sslmode=require");

        Assert.Equal(SslMode.Require, new NpgsqlConnectionStringBuilder(result).SslMode);
    }

    [Fact]
    public void ToConnectionString_ReadsTheHyphenatedSslModes()
    {
        var result = DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb?sslmode=verify-full");

        Assert.Equal(SslMode.VerifyFull, new NpgsqlConnectionStringBuilder(result).SslMode);
    }

    [Fact]
    public void ToConnectionString_ReadsChannelBindingFromTheQueryString()
    {
        var result = DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb?sslmode=require&channel_binding=require");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal(SslMode.Require, builder.SslMode);
        Assert.Equal(ChannelBinding.Require, builder.ChannelBinding);
    }

    [Fact]
    public void ToConnectionString_RejectsAnUnknownSslMode()
    {
        Assert.Throws<FormatException>(() =>
            DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb?sslmode=banana"));
    }

    [Fact]
    public void ToConnectionString_RejectsAnUnknownChannelBinding()
    {
        Assert.Throws<FormatException>(() =>
            DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb?sslmode=require&channel_binding=banana"));
    }
    // Passes before and after on purpose. It proves a URL with no query string keeps Npgsql's defaults.
    [Fact]
    public void ToConnectionString_WithNoQueryString_KeepsTheDefaultSslMode()
    {
        var result = DatabaseUrl.ToConnectionString("postgresql://appuser:secret@db.example.com/appdb");

        Assert.Equal(new NpgsqlConnectionStringBuilder().SslMode, new NpgsqlConnectionStringBuilder(result).SslMode);
    }
}