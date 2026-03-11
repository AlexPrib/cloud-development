var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

const int replicaCount = 3;
var generationServices = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < replicaCount; i++)
{
    var service = builder.AddProject<Projects.GenerationService>($"generation-service-{i}")
        .WithReference(cache)
        .WithEndpoint("http", endpoint => endpoint.Port = 5000 + i)
        .WaitFor(cache);

    generationServices.Add(service);
}

var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithEndpoint("http", endpoint => endpoint.Port = 5100)
    .WithExternalHttpEndpoints();

foreach (var service in generationServices)
{
    apiGateway = apiGateway.WithReference(service).WaitFor(service);
}

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();
