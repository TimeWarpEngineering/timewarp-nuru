#!/usr/bin/env -S dotnet run --launch-profile http --
#:sdk Aspire.AppHost.Sdk@13.1.0

// Aspire Host with OpenTelemetry Sample
// ======================================
// This sample demonstrates:
// - Aspire Dashboard with built-in OTLP receiver for telemetry
// - NuruClient runfile registered as an Aspire-managed C# app
// - Telemetry flows automatically to the Aspire Dashboard
//
// To run:
//   ./apphost.cs
//   (Aspire launches NuruClient automatically)

// Type is for evaluation purposes only and is subject to change or
// removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder();

// Register NuruClient runfile as an Aspire-managed C# app.
// Aspire will:
// - Launch it automatically
// - Inject OTEL_EXPORTER_OTLP_ENDPOINT pointing to the dashboard
// - Show its telemetry in the dashboard
//
// Pass arguments to run a command instead of entering REPL mode
// (REPL requires interactive console which Aspire doesn't provide)
builder.AddCSharpApp("nuruclient", "./nuru-client.cs")
  .WithArgs("status");

await builder.Build().RunAsync();
