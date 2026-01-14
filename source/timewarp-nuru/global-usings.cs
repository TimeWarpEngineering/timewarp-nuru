// System namespaces
global using System;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;

// Microsoft namespaces
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Diagnostics.Metrics;
global using Microsoft.Extensions.FileProviders;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.Options;

// TimeWarp namespaces
global using TimeWarp.Builder;
global using TimeWarp.Nuru;
global using TimeWarp.Terminal;

// Suppress source generator for this library assembly - consuming apps will generate their own invokers
[assembly: SuppressNuruInvokerGeneration]
