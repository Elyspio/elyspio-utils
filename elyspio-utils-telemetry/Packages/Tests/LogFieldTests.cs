using Shouldly;
using Xunit;

namespace Elyspio.Utils.Telemetry.Tests;

public sealed class LogFieldTests
{
	public sealed record Request(string Grid, [property: LogMask] string Token, [property: LogIgnore] string Internal, byte[] Payload, string Label);

	[Fact]
	public void Scalars_are_written_as_json()
	{
		var count = 3;
		var name = "Fer";

		$"{Log.F(count)} {Log.F(name)}".ShouldBe("count=3 name=\"Fer\"");
	}

	[Fact]
	public void Objects_are_masked_ignored_and_summarized()
	{
		var request = new Request("main", "s3cr3t", "hidden", new byte[1_000], "Fer étiré");

		var text = $"{Log.F(request)}";

		text.ShouldBe("request={\"Grid\":\"main\",\"Token\":\"***\",\"Payload\":\"(1000 bytes)\",\"Label\":\"Fer étiré\"}");
	}

	[Fact]
	public void Long_values_are_truncated()
	{
		var items = Enumerable.Range(0, 10_000).ToArray();

		var text = $"{Log.F(items)}";

		text.Length.ShouldBeLessThan(9 * 1024);
	}
}
