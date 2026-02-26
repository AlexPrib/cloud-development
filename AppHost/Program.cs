var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var generationService = builder.AddProject<Projects.GenerationService>("generation-service")
    .WithReference(cache)
    .WaitFor(cache);

builder.AddProject<Projects.Client_Wasm>("client-wasm")
    .WithExternalHttpEndpoints()
    .WaitFor(generationService);

builder.Build().Run();
