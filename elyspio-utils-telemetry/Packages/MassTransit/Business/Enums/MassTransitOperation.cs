namespace Elyspio.Utils.Telemetry.MassTransit.Business.Enums;

/// <summary>
///     Operation type for MassTransit
/// </summary>
public enum MassTransitOperation
{
	/// <summary>
	///     A publish operation
	/// </summary>
	Send,

	/// <summary>
	///     A consume operation
	/// </summary>
	Process,

	/// <summary>
	///     A receive operation
	/// </summary>
	Receive
}