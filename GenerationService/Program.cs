using Amazon.SQS;
using GenerationService;
using GenerationService.Models;
using GenerationService.Services;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using ServiceDefaults;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

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

var sqsServiceUrl = builder.Configuration["Sqs:ServiceUrl"];
if (!string.IsNullOrEmpty(sqsServiceUrl))
{
    builder.Services.AddMassTransit(x =>
    {
        x.UsingAmazonSqs((_, cfg) =>
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
            cfg.UseRawJsonSerializer();
        });
    });
}

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

app.MapGet("/course", async (
    int id,
    IDistributedCache cache,
    IConfiguration configuration,
    ISendEndpointProvider? sendEndpointProvider,
    ILogger<Program> logger) =>
{
    if (id < 0)
        return Results.BadRequest("Received invalid ID. ID must be a non-negative number");

    var cacheKey = $"course:{id}";

    var cached = await cache.GetStringAsync(cacheKey);
    if (cached is not null)
    {
        var cachedCourse = JsonSerializer.Deserialize<Course>(cached);
        if (cachedCourse is not null)
        {
            logger.LogInformation("Cache hit for course with id {CourseId}", id);
            return Results.Ok(cachedCourse);
        }
    }

    logger.LogInformation("Cache miss for course with id {CourseId}, generating new data", id);
    var course = CourseGenerator.Generate(id);

    var cacheExpirationMinutes = configuration.GetValue<int>("Cache:ExpirationMinutes", 10);

    await cache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(course),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationMinutes)
        });

    logger.LogInformation("Generated and cached course {CourseName} with id {CourseId}", course.Name, id);

    if (sendEndpointProvider is not null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:courses"));
                await sendEndpoint.Send(new CourseMessage(
                    course.Id,
                    course.Name,
                    course.TeacherFullName,
                    course.StartDate,
                    course.EndDate,
                    course.MaxCountStudents,
                    course.CurrentCountStudents,
                    course.HasCertificate,
                    course.Cost,
                    course.Rating
                ));
                logger.LogInformation("Sent course {CourseId} to SQS queue", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send course {CourseId} to SQS", id);
            }
        });
    }

    return Results.Ok(course);
});

app.Run();
