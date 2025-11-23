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
  /// Adds console logging with default Nuru configuration.
  /// </summary>
  public static NuruAppBuilder UseConsoleLogging(this NuruAppBuilder builder, LogLevel minimumLevel = LogLevel.Information)
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

    return builder.UseLogging(loggerFactory);
  }

  /// <summary>
  /// Adds console logging with custom configuration.
  /// </summary>
  public static NuruAppBuilder UseConsoleLogging(this NuruAppBuilder builder, Action<ILoggingBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);

#pragma warning disable CA2000 // Dispose objects before losing scope - ILoggerFactory is owned by NuruApp
    ILoggerFactory loggerFactory = LoggerFactory.Create(configure);
#pragma warning restore CA2000

    return builder.UseLogging(loggerFactory);
  }

  /// <summary>
  /// Adds debug console logging (includes Trace level).
  /// </summary>
  public static NuruAppBuilder UseDebugLogging(this NuruAppBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);
    return builder.UseConsoleLogging(LogLevel.Trace);
  }
 }