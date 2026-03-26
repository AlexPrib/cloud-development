using Amazon.SQS;
using GenerationService.Services;
using MassTransit;
using ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<ICourseCacheService, CourseCacheService>();
builder.Services.AddScoped<ICourseService, CourseService>();

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

    builder.Services.AddScoped<ICoursePublisher, CoursePublisher>();
}

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

app.MapGet("/course", async (
    int id,
    ICourseService courseService) =>
{
    if (id < 0)
        return Results.BadRequest("Received invalid ID. ID must be a non-negative number");

    var course = await courseService.GetOrCreateAsync(id);
    return Results.Ok(course);
});

app.Run();
