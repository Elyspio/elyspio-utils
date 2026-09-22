using Elyspio.Utils.Telemetry.Sql.Helpers;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace Elyspio.Utils.Telemetry.Sql.Tests;

public class SqlHelperTests
{
	[Theory]
	[InlineData("SELECT * FROM [dbo].[Users] AS u JOIN [auth].[Roles] r ON 1=1", "SELECT", "dbo.Users", "auth.Roles")]
	[InlineData("WITH recent AS (SELECT * FROM [sales].[Orders]) SELECT * FROM recent JOIN [crm].[Customers] c ON 1=1", "SELECT", "sales.Orders", "crm.Customers")]
	[InlineData("UPDATE [dbo].[Users] SET [Name] = @name WHERE [Id] = @id", "UPDATE", "dbo.Users")]
	[InlineData("INSERT INTO [dbo].[Users] ([Name]) VALUES (@name)", "INSERT", "dbo.Users")]
	public void Extracts_commands_and_tables(string query, string command, params string[] tables)
	{
		SqlHelper.ExtractCommandFromQuery(query).ToUpperInvariant().ShouldBe(command);
		foreach (var table in tables) SqlHelper.ExtractTablesFromQuery(query).ShouldContain(t => string.Equals(t, table, StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Extracts_and_truncates_parameters()
	{
		using var command = new SqlCommand();
		command.Parameters.AddWithValue("@id", 42);
		command.Parameters.AddWithValue("@payload", new string('x', 1200));
		var values = SqlHelper.ExtractParameterValues(command.Parameters);
		values["id"].ShouldBe("42");
		values["payload"].ShouldEndWith("… (truncated, 1200 chars)");
	}
}
