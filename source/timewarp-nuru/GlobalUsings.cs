global using Mediator;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Diagnostics.Metrics;
global using Microsoft.Extensions.FileProviders;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using TimeWarp.Nuru;

// Suppress source generator for this library assembly - consuming apps will generate their own invokers
[assembly: SuppressNuruInvokerGeneration]
