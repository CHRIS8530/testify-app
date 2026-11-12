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
}