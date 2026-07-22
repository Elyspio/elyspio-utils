using Elyspio.Utils.Telemetry.Sql.Helpers;
using Microsoft.Data.SqlClient;
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
		Assert.Equal(command, SqlHelper.ExtractCommandFromQuery(query).ToUpperInvariant());
		foreach (var table in tables) Assert.Contains(table, SqlHelper.ExtractTablesFromQuery(query), StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void Extracts_and_truncates_parameters()
	{
		using var command = new SqlCommand();
		command.Parameters.AddWithValue("@id", 42);
		command.Parameters.AddWithValue("@payload", new string('x', 1200));
		var values = SqlHelper.ExtractParameterValues(command.Parameters);
		Assert.Equal("42", values["id"]);
		Assert.EndsWith("… (truncated, 1200 chars)", values["payload"]);
	}
}
