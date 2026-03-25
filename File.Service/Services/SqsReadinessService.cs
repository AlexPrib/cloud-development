namespace File.Service.Services;

/// <summary>
/// Ожидает готовности SQS-эндпоинта до старта MassTransit.
/// Регистрируется первым, чтобы MassTransit не упал при подключении к ещё не запущенному ElasticMQ.
/// </summary>
public class SqsReadinessService(IConfiguration configuration, ILogger<SqsReadinessService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var sqsUrl = configuration["Sqs:ServiceUrl"] ?? "http://localhost:9324";

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        var deadline = DateTime.UtcNow.AddMinutes(2);

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await httpClient.GetAsync($"{sqsUrl}/?Action=ListQueues", cancellationToken);
                logger.LogInformation("SQS is ready at {Url}", sqsUrl);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "SQS not ready at {Url}, retrying in 2 seconds...", sqsUrl);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        logger.LogError("SQS did not become ready within the timeout");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
