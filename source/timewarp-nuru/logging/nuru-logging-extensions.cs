namespace TimeWarp.Nuru;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

/// <summary>
/// Extension methods for configuring logging in Nuru applications.
/// </summary>
public static class NuruLoggingExtensions
{
  /// <summary>
  /// Configures logging using the provided action.
  /// Creates an ILoggerFactory from the configuration and passes it to the builder.
  /// </summary>
  /// <param name="builder">The Nuru app builder.</param>
  /// <param name="configure">The action to configure logging.</param>
  /// <returns>The builder for chaining.</returns>
  /// <example>
  /// <code>
  /// NuruApp app = NuruApp.CreateBuilder([])
  ///   .ConfigureLogging(logging =>
  ///   {
  ///     logging.SetMinimumLevel(LogLevel.Debug);
  ///     logging.AddConsole();
  ///     logging.AddOpenTelemetry(otel => otel.AddOtlpExporter());
  ///   })
  ///   .Map("hello", () => Console.WriteLine("Hello!"))
  ///   .Build();
  /// </code>
  /// </example>
  public static TBuilder ConfigureLogging<TBuilder>(this TBuilder builder, Action<ILoggingBuilder> configure)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);

#pragma warning disable CA2000 // Dispose objects before losing scope - ILoggerFactory is owned by NuruApp
    ILoggerFactory loggerFactory = LoggerFactory.Create(configure);
#pragma warning restore CA2000

    builder.UseLogging(loggerFactory);
    return builder;
  }

  /// <summary>
  /// Adds console logging with default Nuru configuration.
  /// </summary>
  public static TBuilder UseConsoleLogging<TBuilder>(this TBuilder builder, LogLevel minimumLevel = LogLevel.Information)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);

#pragma warning disable CA2000 // Dispose objects before losing scope - ILoggerFactory is owned by NuruApp
    ILoggerFactory loggerFactory =
      LoggerFactory.Create
      (
        logging =>
          logging
          .SetMinimumLevel(minimumLevel)
          .AddSimpleConsole
          (
            options =>
            {
              options.IncludeScopes = false;
              options.TimestampFormat = "HH:mm:ss ";
              options.SingleLine = true;
            }
          )
      );
#pragma warning restore CA2000

    builder.UseLogging(loggerFactory);
    return builder;
  }

  /// <summary>
  /// Adds console logging with custom configuration.
  /// </summary>
  public static TBuilder UseConsoleLogging<TBuilder>(this TBuilder builder, Action<ILoggingBuilder> configure)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);

#pragma warning disable CA2000 // Dispose objects before losing scope - ILoggerFactory is owned by NuruApp
    ILoggerFactory loggerFactory = LoggerFactory.Create(configure);
#pragma warning restore CA2000

    builder.UseLogging(loggerFactory);
    return builder;
  }

  /// <summary>
  /// Adds debug console logging (includes Trace level).
  /// </summary>
  public static TBuilder UseDebugLogging<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    return builder.UseConsoleLogging(LogLevel.Trace);
  }
}
