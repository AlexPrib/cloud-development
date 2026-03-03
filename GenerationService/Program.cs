using ServiceDefaults;
using GenerationService.Models;
using GenerationService.Services;
using Microsoft.Extensions.Caching.Distributed;
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

var app = builder.Build();

app.UseCors();
app.MapDefaultEndpoints();

app.MapGet("/course", async (int id, IDistributedCache cache, IConfiguration configuration, ILogger<Program> logger) =>
{
    if (id <= 0)
        return Results.BadRequest("Received invalid ID. ID must be a positive number");

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

    logger.LogInformation(
        "Generated and cached course {CourseName} with id {CourseId}",
        course.Name, id);

    return Results.Ok(course);
});

app.Run();
