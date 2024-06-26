using System.Diagnostics;
using Elyspio.Utils.Telemetry.MassTransit.Business.Enums;

namespace Elyspio.Utils.Telemetry.MassTransit.Helpers;

/// <summary>
///     Helper for MassTransit activities
/// </summary>
public static class MassTransitActivityHelper
{
	/// <summary>
	///     Get activity name for MassTransit activities from generic type
	/// </summary>
	/// <param name="operation"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	private static string GetActivityName<T>(MassTransitOperation operation)
	{
		return $"RabbitMQ - {operation} - {typeof(T).Name}";
	}

	/// <summary>
	///     Get activity name for MassTransit activities from class name
	/// </summary>
	/// <param name="operation"></param>
	/// <param name="className"></param>
	/// <returns></returns>
	public static string GetActivityName(MassTransitOperation operation, string className)
	{
		return $"RabbitMQ - {operation} - {className}";
	}

	/// <summary>
	///     Set activity name for MassTransit activities from generic type
	/// </summary>
	/// <param name="operation"></param>
	/// <typeparam name="T"></typeparam>
	public static void SetActivityName<T>(MassTransitOperation operation)
	{
		var activity = Activity.Current;

		if (activity is not null) activity.DisplayName = GetActivityName<T>(operation);
	}
}