using Ocelot.Errors;

namespace ApiGateway;

public class NoServicesAvailableError(string message) : Error(message, OcelotErrorCode.UnknownError, 503) { }
