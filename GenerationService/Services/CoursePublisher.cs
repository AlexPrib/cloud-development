using GenerationService.Models;
using MassTransit;

namespace GenerationService.Services;

public class CoursePublisher(
    ISendEndpointProvider sendEndpointProvider,
    ILogger<CoursePublisher> logger) : ICoursePublisher
{
    public Task PublishAsync(Course course, CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            try
            {
                var sendEndpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:courses"));
                await sendEndpoint.Send(course, cancellationToken);

                logger.LogInformation("Sent course {CourseId} to SQS queue", course.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send course {CourseId} to SQS", course.Id);
            }
        }, cancellationToken);
    }
}
