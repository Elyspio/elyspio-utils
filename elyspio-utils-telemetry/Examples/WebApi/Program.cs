using System.Text.Json.Serialization;
using Elyspio.Utils.Telemetry.Examples.WebApi.Services;
using Elyspio.Utils.Telemetry.Tracing.Builder;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Repositories;
using Elyspio.Utils.Telemetry.Examples.WebApi.Abstractions.Interfaces.Services;
using Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Mongo;
using Elyspio.Utils.Telemetry.Examples.WebApi.Repositories.Sql;
using Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Filters;
using Elyspio.Utils.Telemetry.Examples.WebApi.Rest.Middlewares;
using Elyspio.Utils.Telemetry.MongoDB.Extensions;
using Elyspio.Utils.Telemetry.Redis.Extensions;
using Elyspio.Utils.Telemetry.Sql.Extensions;
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilogWithTelemetry();

builder.Services.AddSqlServer<AppSqlContext>(builder.Configuration.GetConnectionString("sql") ?? builder.Configuration["Sql"]);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.CustomOperationIds(op => op.ActionDescriptor.RouteValues["controller"] + op.ActionDescriptor.RouteValues["action"]); });


builder.Services.AddScoped<FakeMiddleware>();

builder.Services
	.AddControllers(o => { o.Filters.Add<HttpExceptionActionFilter>(); })
	.AddJsonOptions(o => { o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });


#region Redis

// On l'ajoute en tant que singleton afin qu'opentelemetry puisse l'utiliser pour récupérer les traces
var redisConfiguration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("redis") ?? "localhost:6379");
redisConfiguration.ClientName = "aura-local-telemetry-webapi";

var redisConnectionMultiplexer = ConnectionMultiplexer.Connect(redisConfiguration);

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnectionMultiplexer);
builder.Services.AddStackExchangeRedisCache(options => options.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnectionMultiplexer as IConnectionMultiplexer));

#endregion Redis


if (builder.Configuration.IsTelemetryEnabled(out var telemetryOptions))
{
	var telemetryBuilder = new AppOpenTelemetryBuilder<Program>(telemetryOptions!, builder.Configuration)
	{
		Tracing = (tracing, _) => tracing
			.AddAppMongoInstrumentation()
			.AddAppSqlClientInstrumentation()
			.AddAppRedisInstrumentation()
	};


	telemetryBuilder.Build(builder.Services);
	builder.Services.AddOpenTelemetryJsonConfiguration(builder.Configuration);
}


var app = builder.Build();

app.UseOpenTelemetryJsonConfiguration();


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
