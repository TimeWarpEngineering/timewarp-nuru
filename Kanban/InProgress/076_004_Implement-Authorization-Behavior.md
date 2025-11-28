# Implement Authorization Behavior

## Description

Create an AuthorizationBehavior that checks permissions using a marker interface pattern, demonstrating selective behavior application.

## Parent

076_Add-Pipeline-Middleware-Sample

## Checklist

- [x] Create IRequireAuthorization marker interface with RequiredPermission property
- [x] Create AuthorizationBehavior<TRequest, TResponse> class
- [x] Check if request implements IRequireAuthorization
- [x] Validate permission (e.g., via environment variable or config)
- [x] Throw UnauthorizedAccessException on failure
- [x] Create sample command implementing IRequireAuthorization
- [x] Create sample command without authorization requirement

## Notes

```csharp
public interface IRequireAuthorization
{
    string RequiredPermission { get; }
}

public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest
{
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> Logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IRequireAuthorization authRequest)
        {
            Logger.LogInformation("Checking permission: {Permission}", authRequest.RequiredPermission);
            // Simple demo: check environment variable
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLI_AUTHORIZED")))
                throw new UnauthorizedAccessException($"Permission required: {authRequest.RequiredPermission}");
        }
        return await next();
    }
}
```

Demonstrates marker interface pattern for selective behavior application.
