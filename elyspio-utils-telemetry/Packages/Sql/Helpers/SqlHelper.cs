using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Elyspio.Utils.Telemetry.Sql.Helpers;

internal static partial class SqlHelper
{
	private const int MaxParameterLength = 1_024;
	private static readonly Regex TablePattern = TableRegex();
	private static readonly Regex CtePattern = CteRegex();

	internal static List<string> ExtractTablesFromQuery(ReadOnlySpan<char> query)
	{
		try
		{
			var sql = StripLeadingSetStatements(query.ToString());
			var ctes = CtePattern.Matches(sql).Select(match => Normalize(match.Groups[1].Value)).ToHashSet(StringComparer.OrdinalIgnoreCase);
			return TablePattern.Matches(sql).Select(match => Normalize(match.Groups[1].Value))
				.Where(table => !string.IsNullOrEmpty(table) && !ctes.Contains(table)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch { return []; }
	}

	internal static string ExtractCommandFromQuery(ReadOnlySpan<char> query)
	{
		var sql = StripLeadingSetStatements(query.ToString());
		var cte = Regex.Match(sql, @"^\s*WITH\b[\s\S]*?\)\s*(SELECT|INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase);
		if (cte.Success) return cte.Groups[1].Value;
		var command = Regex.Match(sql, @"\b(SELECT|INSERT|UPDATE|DELETE|MERGE)\b", RegexOptions.IgnoreCase);
		return command.Success ? command.Groups[1].Value : string.Empty;
	}

	internal static Dictionary<string, string> ExtractParameterValues(SqlParameterCollection parameters)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (SqlParameter parameter in parameters)
		{
			var key = parameter.ParameterName.TrimStart('@', ':', '?');
			if (!string.IsNullOrWhiteSpace(key)) result[key] = Format(parameter.Value);
		}
		return result;
	}

	private static string StripLeadingSetStatements(string sql)
	{
		while (Regex.IsMatch(sql, @"^\s*SET\b[\s\S]*?;", RegexOptions.IgnoreCase)) sql = Regex.Replace(sql, @"^\s*SET\b[\s\S]*?;", string.Empty, RegexOptions.IgnoreCase);
		return sql;
	}

	private static string Normalize(string value) => string.Join('.', value.Split('.').Select(part => part.Trim().Trim('[', ']', '"')));
	private static string Format(object? value)
	{
		var text = value is null or DBNull ? "null" : value switch { DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture), DateTime date => date.ToString("O", CultureInfo.InvariantCulture), _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null" };
		return text.Length <= MaxParameterLength ? text : $"{text[..MaxParameterLength]}… (truncated, {text.Length} chars)";
	}

	[GeneratedRegex(@"\b(?:FROM|JOIN|INTO|UPDATE|MERGE\s+INTO|DELETE\s+FROM)\s+((?:\[[^]]+\]|[\w]+)(?:\s*\.\s*(?:\[[^]]+\]|[\w]+))*)", RegexOptions.IgnoreCase)]
	private static partial Regex TableRegex();
	[GeneratedRegex(@"(?:^|,)\s*((?:\[[^]]+\]|[\w]+)(?:\s*\.\s*(?:\[[^]]+\]|[\w]+))*)\s*(?:\([^)]*\))?\s+AS\s*\(", RegexOptions.IgnoreCase)]
	private static partial Regex CteRegex();
}
