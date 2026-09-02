#!/usr/bin/env -S dotnet --
#:sdk Aspire.AppHost.Sdk@13.5.3
#:property NoWarn=ASPIRE004;ASPIRECSHARPAPPS001;ASPIRETERMINAL001

// Aspire Host with OpenTelemetry Sample
// ======================================
// This sample demonstrates:
// - Aspire Dashboard with built-in OTLP receiver for telemetry
// - NuruClient runfile registered as an Aspire-managed C# app
// - Telemetry flows automatically to the Aspire Dashboard
// - Interactive Nuru REPL under Aspire 13.5 WithTerminal() PTY
//
// To run:
//   cd samples/aspire-otel
//   aspire config set features.terminalCommandsEnabled true
//   aspire run
//   (Dashboard Terminal view, or: aspire terminal attach nuruclient)

// Type is for evaluation purposes only and is subject to change or
// removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECSHARPAPPS001
#pragma warning disable ASPIRETERMINAL001

var builder = DistributedApplication.CreateBuilder();

// Register NuruClient runfile as an Aspire-managed C# app.
// Aspire will:
// - Launch it automatically under a PTY (WithTerminal)
// - Inject OTEL_EXPORTER_OTLP_ENDPOINT pointing to the dashboard
// - Show its telemetry in the dashboard
// - Expose a live Terminal view and `aspire terminal attach`
builder.AddCSharpApp("nuruclient", "./nuru-client.cs")
  // "--" so --interactive is the app's flag, not `dotnet run --interactive`.
  .WithArgs("--", "--interactive")
  .WithTerminal();

await builder.Build().RunAsync();
