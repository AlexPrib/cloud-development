using Ocelot.Configuration.File;

namespace ApiGateway;

public class OcelotConfigurationService(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public FileConfiguration BuildConfiguration()
    {
        var downstreamHostAndPorts = new List<FileHostAndPort>();
        var servicesSection = _configuration.GetSection("services");
        var index = 0;
        
        while (true)
        {
            var serviceName = $"generation-service-{index}";
            var serviceUrl = servicesSection[$"{serviceName}:http:0"];
            
            if (string.IsNullOrEmpty(serviceUrl))
                break;
            
            if (Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            {
                downstreamHostAndPorts.Add(new FileHostAndPort
                {
                    Host = uri.Host,
                    Port = uri.Port
                });
            }
            
            index++;
        }

        return new FileConfiguration
        {
            Routes =
            [
                new FileRoute
                {
                    DownstreamPathTemplate = "/course",
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/course",
                    UpstreamHttpMethod = ["Get"],
                    DownstreamHostAndPorts = downstreamHostAndPorts,
                    LoadBalancerOptions = new FileLoadBalancerOptions
                    {
                        Type = nameof(QueryBasedLoadBalancer)
                    }
                }
            ],
            GlobalConfiguration = new FileGlobalConfiguration()
        };
    }
}
