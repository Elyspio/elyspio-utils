namespace Elyspio.Utils.Telemetry.Examples.WebApi.MassTransit.Messages;

[Serializable]
public sealed record ToggleTodoMessage(Guid IdTodo);