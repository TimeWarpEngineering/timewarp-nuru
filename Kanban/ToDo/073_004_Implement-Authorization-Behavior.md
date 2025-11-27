# Implement Authorization Behavior

## Description

Create an AuthorizationBehavior that checks permissions using a marker interface pattern, demonstrating selective behavior application.

## Parent

073_Add-Pipeline-Middleware-Sample

## Checklist

- [ ] Create IRequireAuthorization marker interface with RequiredPermission property
- [ ] Create AuthorizationBehavior<TRequest, TResponse> class
- [ ] Check if request implements IRequireAuthorization
- [ ] Validate permission (e.g., via environment variable or config)
- [ ] Throw UnauthorizedAccessException on failure
- [ ] Create sample command implementing IRequireAuthorization
- [ ] Create sample command without authorization requirement

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
