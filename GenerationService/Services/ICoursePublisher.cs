using GenerationService.Models;

namespace GenerationService.Services;

public interface ICoursePublisher
{
    public Task PublishAsync(Course course, CancellationToken cancellationToken = default);
}
