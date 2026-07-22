var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var mongo = builder.AddMongoDB("mongodb")
	.WithImageTag("8.0.4")
	.AddDatabase("mongo");
var sql = builder.AddSqlServer("sqlserver").AddDatabase("sql");

builder.AddProject<Projects.Elyspio_Utils_Telemetry_Examples_WebApi>("webapi")
	.WithReference(redis)
	.WithReference(mongo)
	.WithReference(sql)
	.WaitFor(redis)
	.WaitFor(mongo)
	.WaitFor(sql);

builder.Build().Run();
