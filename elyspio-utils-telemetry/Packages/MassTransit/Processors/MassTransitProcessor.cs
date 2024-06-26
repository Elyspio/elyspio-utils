using System.Diagnostics;
using Elyspio.Utils.Telemetry.MassTransit.Business.Enums;
using Elyspio.Utils.Telemetry.MassTransit.Helpers;
using MassTransit.Monitoring;
using Microsoft.Extensions.Options;
using OpenTelemetry;

namespace Elyspio.Utils.Telemetry.MassTransit.Processors;

/// <summary>
///     Change activity name for MassTransit activities with <see cref="MassTransitActivityHelper" />
/// </summary>
public sealed class MassTransitProcessor : BaseProcessor<Activity>
{
	private readonly IOptionsMonitor<InstrumentationOptions> _optionsMonitor;

	/// <summary>
	///    Constructor
	/// </summary>
	/// <param name="optionsMonitor"></param>
	public MassTransitProcessor(IOptionsMonitor<InstrumentationOptions> optionsMonitor)
	{
		_optionsMonitor = optionsMonitor;
	}

	/// <exception cref="ArgumentOutOfRangeException"></exception>
	/// <inheritdoc />
	public override void OnEnd(Activity activity)
	{
		try
		{
			if (activity.Source.Name != InstrumentationOptions.MeterName) return;


			var operationRaw = activity.Tags.FirstOrDefault(x => x is { Key: "messaging.operation" });

			if (operationRaw is { Value: null }) return;

			MassTransitOperation? operation = operationRaw.Value switch
			{
				"send" => MassTransitOperation.Send,
				"process" => MassTransitOperation.Process,
				"receive" => MassTransitOperation.Receive,
				_ => null
			};

			var tags = activity.Tags.ToDictionary();
			string? messageType;
			switch (operationRaw.Value)
			{
				case "send":
					messageType = tags.GetValueOrDefault("messaging.destination.name");
					break;
				case "process":
					messageType = tags.GetValueOrDefault(_optionsMonitor.CurrentValue.ConsumerTypeLabel);
					messageType = messageType?[(messageType.LastIndexOf('.') + 1)..];
					break;
				case "receive":
					messageType = tags.GetValueOrDefault("messaging.destination.name");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (operation is null) return;
			if (messageType is null) return;

			var className = messageType[(messageType.LastIndexOf(':') + 1)..];

			activity.DisplayName = MassTransitActivityHelper.GetActivityName(operation.Value, className);
		}
		catch (Exception e)
		{
			activity.SetStatus(ActivityStatusCode.Error);
			activity.SetTag("exception", e);
		}
	}
}