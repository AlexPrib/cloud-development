using ApiGateway;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
    }
    else
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
    }
});

var ocelotConfigService = new OcelotConfigurationService(builder.Configuration);
var ocelotConfig = ocelotConfigService.BuildConfiguration();

builder.Services.Configure<FileConfiguration>(config =>
{
    config.Routes = ocelotConfig.Routes;
    config.GlobalConfiguration = ocelotConfig.GlobalConfiguration;
});

builder.Services
    .AddOcelot()
    .AddCustomLoadBalancer((serviceProvider, route, serviceDiscoveryProvider) =>
    {
        return new QueryBasedLoadBalancer(serviceDiscoveryProvider.GetAsync);
    });

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

await app.UseOcelot();

app.Run();

