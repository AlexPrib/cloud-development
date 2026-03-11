using Ocelot.LoadBalancer.Interfaces;
using Ocelot.Responses;
using Ocelot.Values;

namespace ApiGateway;

public class QueryBasedLoadBalancer(Func<Task<List<Service>>> services) : ILoadBalancer
{
    public string Type => nameof(QueryBasedLoadBalancer);

    public void Release(ServiceHostAndPort hostAndPort) { }

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var servicesList = await services();

        if (servicesList == null || servicesList.Count == 0)
        {
            return new ErrorResponse<ServiceHostAndPort>(
                new NoServicesAvailableError("No services available for load balancing"));
        }

        if (!httpContext.Request.Query.TryGetValue("id", out var idValues)
            || !int.TryParse(idValues.FirstOrDefault(), out var id))
        {
            var firstService = servicesList[0];
            return new OkResponse<ServiceHostAndPort>(firstService.HostAndPort);
        }

        var replicaIndex = id % servicesList.Count;
        var selectedService = servicesList[replicaIndex];

        return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
    }
}
