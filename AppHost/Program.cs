var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithHttpHealthCheck("/minio/health/live", endpointName: "api");

var sqs = builder.AddContainer("elasticmq", "softwaremill/elasticmq-native")
    .WithHttpEndpoint(targetPort: 9324, name: "http")
    .WithHttpHealthCheck("/?Action=ListQueues", endpointName: "http");

const int replicaCount = 3;
var generationServices = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < replicaCount; i++)
{
    var service = builder.AddProject<Projects.GenerationService>($"generation-service-{i}")
        .WithReference(cache)
        .WithEndpoint("http", endpoint => endpoint.Port = 5000 + i)
        .WaitFor(cache)
        .WithEnvironment("Sqs__ServiceUrl", sqs.GetEndpoint("http"))
        .WaitFor(sqs);

    generationServices.Add(service);
}

var fileService = builder.AddProject<Projects.File_Service>("file-service")
    .WithEnvironment("Sqs__ServiceUrl", sqs.GetEndpoint("http"))
    .WithEnvironment("Minio__ServiceUrl", minio.GetEndpoint("api"))
    .WithEnvironment("Minio__AccessKey", "minioadmin")
    .WithEnvironment("Minio__SecretKey", "minioadmin")
    .WaitFor(sqs)
    .WaitFor(minio);

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
