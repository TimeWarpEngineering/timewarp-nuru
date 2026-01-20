# Configuration

Demonstrates `IOptions<T>` binding and configuration patterns.

## Run It

```bash
dotnet run samples/09-configuration/01-configuration-basics.cs -- config show
dotnet run samples/09-configuration/01-configuration-basics.cs -- db connect
dotnet run samples/09-configuration/02-command-line-overrides.cs -- show --Database:Host=myserver
dotnet run samples/09-configuration/03-configuration-validation.cs -- validate
```

## What's Demonstrated

- **01-basics**: `IOptions<T>` injection from settings files
- **02-command-line-overrides**: Overriding config via command line
- **03-validation**: Configuration validation with data annotations
- **04-user-secrets**: User secrets integration for sensitive data

## Related Documentation

- [Configuration](../../documentation/user/features/configuration.md)
