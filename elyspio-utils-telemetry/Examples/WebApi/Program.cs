using System.Text.Json.Serialization;
using Coexya.Utils.Telemetry.Examples.WebApi.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.ApiSante.Rest;
using Elyspio.Utils.Telemetry.Examples.WebApi.MassTransit.Consumers;
using Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo;
using Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Sql;
using Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Filters;
using Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Middlewares;
using Elyspio.Utils.Telemetry.Examples.WebApi.Services;
using Elyspio.Utils.Telemetry.MassTransit.Extensions;
using Elyspio.Utils.Telemetry.MongoDB.Extensions;
using Elyspio.Utils.Telemetry.Redis.Extensions;
using Elyspio.Utils.Telemetry.Sql.Extensions;
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Elyspio.Utils.Telemetry.Tracing.Builder;
using MassTransit;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSqlServer<AppSqlContext>(builder.Configuration["Sql"]);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

builder.Services.AddHttpClient<ApiSanteRestClient>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
	o.CustomOperationIds(op => op.ActionDescriptor.RouteValues["controller"] + op.ActionDescriptor.RouteValues["action"]);
});


builder.Services.AddMassTransit(x =>
{
	x.AddConsumer<ToggleTodoConsumer>();
	x.UsingRabbitMq((context, cfg) =>
	{
		cfg.Host("localhost", "/", h =>
		{
			h.Username("guest");
			h.Password("guest");
		});
		cfg.ConfigureEndpoints(context);
	});
});


builder.Services.AddScoped<FakeMiddleware>();

builder.Services
	.AddControllers(o => { o.Filters.Add<HttpExceptionActionFilter>(); })
	.AddJsonOptions(o =>
	{
		o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});


#region Redis

// On l'ajoute en tant que singleton afin qu'opentelemetry puisse l'utiliser pour récupérer les traces
var redisConnectionMultiplexer = ConnectionMultiplexer.Connect(new ConfigurationOptions
{
	EndPoints =
	{
		"localhost:6379"
	},
	ClientName = "aura-local-telemetry-webapi"
});

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnectionMultiplexer);
builder.Services.AddStackExchangeRedisCache(options => options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnectionMultiplexer as IConnectionMultiplexer));

#endregion Redis


if (builder.Configuration.IsTelemetryEnabled(out var telemetryOptions))
{
	var telemetryBuilder = new AppOpenTelemetryBuilder<Program>(telemetryOptions!)
	{
		Tracing = tracing => tracing
			.AddAppMongoInstrumentation()
			.AddAppSqlClientInstrumentation()
			.AddAppRedisInstrumentation()
			.AddAppMassTransitInstrumentation(),
		Meter = meter => meter.AddAppMassTransitInstrumentation()
	};


	telemetryBuilder.Build(builder.Services);
}


var app = builder.Build();



app.UseSerilogRequestLogging();
app.UseMiddleware<FakeMiddleware>();

var scope = app.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppSqlContext>();
dbContext.Database.EnsureCreated();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Logger.LogInformation($"API started, swagger available at {app.Configuration[WebHostDefaults.ServerUrlsKey]}/swagger/index.html");

app.Run();