using GenerationService.Models;

namespace GenerationService.Services;

public interface ICourseCacheService
{
    public Task<Course?> GetAsync(int id, CancellationToken cancellationToken = default);
    public Task SetAsync(Course course, CancellationToken cancellationToken = default);
}
