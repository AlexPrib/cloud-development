using GenerationService.Models;

namespace GenerationService.Services;

public class CourseService(
    ICourseCacheService cacheService,
    ILogger<CourseService> logger,
    ICoursePublisher? coursePublisher = null) : ICourseService
{
    public async Task<Course> GetOrCreateAsync(int id, CancellationToken cancellationToken = default)
    {
        var cachedCourse = await cacheService.GetAsync(id, cancellationToken);
        if (cachedCourse is not null)
        {
            return cachedCourse;
        }

        logger.LogInformation("Cache miss for course with id {CourseId}, generating new data", id);
        var course = CourseGenerator.Generate(id);

        await cacheService.SetAsync(course, cancellationToken);

        logger.LogInformation("Generated and cached course {CourseName} with id {CourseId}", course.Name, id);

        if (coursePublisher is not null)
        {
            _ = coursePublisher.PublishAsync(course, cancellationToken);
        }

        return course;
    }
}
