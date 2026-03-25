using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using File.Service.Services;
using MassTransit;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var minioServiceUrl = builder.Configuration["Minio:ServiceUrl"] ?? "http://localhost:9000";
var minioAccessKey = builder.Configuration["Minio:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["Minio:SecretKey"] ?? "minioadmin";

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    new BasicAWSCredentials(minioAccessKey, minioSecretKey),
    new AmazonS3Config
    {
        ServiceURL = minioServiceUrl,
        ForcePathStyle = true,
        AuthenticationRegion = "us-east-1"
    }
));

var sqsServiceUrl = builder.Configuration["Sqs:ServiceUrl"] ?? "http://localhost:9324";

builder.Services.AddHostedService<SqsReadinessService>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CourseConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host("us-east-1", h =>
        {
            h.AccessKey("test");
            h.SecretKey("test");
            h.Config(new AmazonSQSConfig
            {
                ServiceURL = sqsServiceUrl,
                AuthenticationRegion = "us-east-1"
            });
        });

        cfg.ReceiveEndpoint("courses", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.UseRawJsonDeserializer(RawSerializerOptions.AnyMessageType);
            e.Consumer<CourseConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<MinioInitializer>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.Run();
