using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.SetBasePath(builder.Environment.ContentRootPath)
	.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
	.AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.Use(async (context, next) =>
{
	if (!context.Request.Headers.ContainsKey("X-Client-Id"))
	{
		var fallbackClientId = context.Connection.RemoteIpAddress?.ToString();
		if (string.IsNullOrWhiteSpace(fallbackClientId))
		{
			fallbackClientId = "anonymous-client";
		}

		context.Request.Headers["X-Client-Id"] = $"{fallbackClientId}-gateway-default-v2";
	}

	await next();
});

await app.UseOcelot();

app.Run();
