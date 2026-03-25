using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using System.Text.Json;

namespace File.Service.Services;

public class CourseConsumer(
    IAmazonS3 s3Client,
    ILogger<CourseConsumer> logger) : IConsumer<CourseMessage>
{
    private const string BucketName = "courses";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Consume(ConsumeContext<CourseMessage> context)
    {
        var course = context.Message;
        var fileName = $"course-{course.Id}.json";
        var json = JsonSerializer.Serialize(course, _jsonOptions);

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = fileName,
            ContentBody = json,
            ContentType = "application/json"
        }, context.CancellationToken);

        logger.LogInformation("Saved course {CourseId} to Minio as {FileName}", course.Id, fileName);
    }
}
