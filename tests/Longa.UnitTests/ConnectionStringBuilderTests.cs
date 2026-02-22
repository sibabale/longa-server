using Longa.Infrastructure;
using Xunit;

namespace Longa.UnitTests;

public class ConnectionStringBuilderTests
{
    [Fact]
    public void NormalizeForNpgsql_ConvertsPostgresUrl_ToKeyValueFormat()
    {
        var url = "postgresql://user:pass@host.example.com:5432/mydb?sslmode=require";
        var result = ConnectionStringBuilder.NormalizeForNpgsql(url);

        Assert.Contains("Host=host.example.com", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=mydb", result);
        Assert.Contains("Username=user", result);
        Assert.Contains("Password=pass", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void NormalizeForNpgsql_HandlesUrlWithTruncatedQueryString()
    {
        var url = "postgresql://neondb_owner:npg_mneckjsoqr39@ep-polished-glade.example.com/neondb?sslmode";
        var result = ConnectionStringBuilder.NormalizeForNpgsql(url);

        Assert.Contains("Host=ep-polished-glade.example.com", result);
        Assert.Contains("Database=neondb", result);
        Assert.Contains("Username=neondb_owner", result);
        Assert.Contains("Password=npg_mneckjsoqr39", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void NormalizeForNpgsql_DefaultsPortTo5432_WhenOmitted()
    {
        var url = "postgresql://u:p@host/db";
        var result = ConnectionStringBuilder.NormalizeForNpgsql(url);

        Assert.Contains("Port=5432", result);
    }

    [Fact]
    public void NormalizeForNpgsql_ReturnsNonUrlConnectionString_AsIs()
    {
        var keyValue = "Host=localhost;Database=test;Username=u;Password=p;SSL Mode=Require";
        var result = ConnectionStringBuilder.NormalizeForNpgsql(keyValue);

        Assert.Equal(keyValue, result);
    }

    [Fact]
    public void NormalizeForNpgsql_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => ConnectionStringBuilder.NormalizeForNpgsql(""));
        Assert.Throws<ArgumentException>(() => ConnectionStringBuilder.NormalizeForNpgsql("   "));
    }
}
