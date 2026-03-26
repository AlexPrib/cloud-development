using GenerationService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace GenerationService.Services;

public class CourseCacheService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<CourseCacheService> logger) : ICourseCacheService
{
    public async Task<Course?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(id);
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);

        if (cached is null)
            return null;

        var course = JsonSerializer.Deserialize<Course>(cached);
        if (course is not null)
        {
            logger.LogInformation("Cache hit for course with id {CourseId}", id);
        }

        return course;
    }

    public async Task SetAsync(Course course, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(course.Id);
        var cacheExpirationMinutes = configuration.GetValue<int>("Cache:ExpirationMinutes", 10);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(course),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationMinutes)
            },
            cancellationToken);

        logger.LogInformation("Cached course {CourseName} with id {CourseId}", course.Name, course.Id);
    }

    private static string GetCacheKey(int id) => $"course:{id}";
}
