using Amazon.S3;
using Amazon.S3.Model;

namespace File.Service.Services;

public class MinioInitializer(IAmazonS3 s3Client, ILogger<MinioInitializer> logger) : BackgroundService
{
    private const string BucketName = "courses";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = BucketName }, stoppingToken);
                logger.LogInformation("Minio bucket '{BucketName}' ready", BucketName);
                return;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                logger.LogInformation("Minio bucket '{BucketName}' already exists", BucketName);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Failed to initialize Minio bucket, retrying in 3 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
