using GenerationService.Models;

namespace GenerationService.Services;

public interface ICourseService
{
    public Task<Course> GetOrCreateAsync(int id, CancellationToken cancellationToken = default);
}
