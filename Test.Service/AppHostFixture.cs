using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Service;

/// <summary>
/// Фикстура, поднимающая Aspire AppHost один раз для всех интеграционных тестов.
/// </summary>
public class AppHostFixture : IAsyncLifetime
{
    public DistributedApplication App { get; private set; } = null!;
    public AmazonS3Client S3Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
                options.Retry.MaxRetryAttempts = 10;
                options.Retry.Delay = TimeSpan.FromSeconds(3);
            }));

        App = await appHost.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications
            .WaitForResourceHealthyAsync("api-gateway")
            .WaitAsync(TimeSpan.FromMinutes(5));

        using var minioClient = App.CreateHttpClient("minio", "api");
        var minioUrl = minioClient.BaseAddress!.ToString().TrimEnd('/');

        S3Client = new AmazonS3Client(
            new BasicAWSCredentials("minioadmin", "minioadmin"),
            new AmazonS3Config
            {
                ServiceURL = minioUrl,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1"
            });
    }

    /// <summary>
    /// Ожидает появления файла в Minio по указанному ключу.
    /// </summary>
    public async Task<List<S3Object>> WaitForS3ObjectAsync(string key, int maxAttempts = 15)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var response = await S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = "courses",
                Prefix = key
            });

            if (response.S3Objects.Count > 0)
                return response.S3Objects;
        }

        return [];
    }

    public async Task DisposeAsync()
    {
        S3Client?.Dispose();

        try
        {
            await App.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException) { }
        catch (OperationCanceledException) { }
    }
}
