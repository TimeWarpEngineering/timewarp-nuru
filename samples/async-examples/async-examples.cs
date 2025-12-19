#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

// Create builder - uses CreateSlimBuilder for delegate-only patterns
NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder(args);

// Simple async route without parameters
builder.Map("ping")
    .WithHandler(async () =>
    {
        await Task.Delay(100);
        Console.WriteLine("Pong!");
    })
    .AsQuery()
    .Done();

// Async route with required parameter
builder.Map("fetch {url}")
    .WithHandler(async (string url) =>
    {
        Console.WriteLine($"Fetching data from {url}...");
        await Task.Delay(500); // Simulate HTTP request
        Console.WriteLine($"Data fetched from {url}");
    })
    .AsQuery()
    .Done();

// Async route with optional parameter
builder.Map("download {file} {destination?}")
    .WithHandler(async (string file, string? destination) =>
    {
        Console.WriteLine($"Downloading {file}...");
        await Task.Delay(1000); // Simulate download
        
        if (destination is not null)
        {
            Console.WriteLine($"Downloaded {file} to {destination}");
        }
        else
        {
            Console.WriteLine($"Downloaded {file} to default location");
        }
    })
    .AsCommand()
    .Done();

// Async route with typed optional parameter
builder.Map("wait {seconds:int?}")
    .WithHandler(async (int? seconds) =>
    {
        int waitTime = seconds ?? 1;
        Console.WriteLine($"Waiting for {waitTime} seconds...");
        await Task.Delay(waitTime * 1000);
        Console.WriteLine("Done waiting!");
    })
    .AsQuery()
    .Done();

// Async route returning Task<int>
builder.Map("process {count:int}")
    .WithHandler(async (int count) =>
    {
        Console.WriteLine($"Processing {count} items...");
        for (int i = 1; i <= count; i++)
        {
            await Task.Delay(100);
            Console.WriteLine($"  Processed item {i}/{count}");
        }
        return count; // This will be the exit code
    })
    .AsCommand()
    .Done();

// Async route with error handling
builder.Map("risky {operation}")
    .WithHandler(async (string operation) =>
    {
        try
        {
            Console.WriteLine($"Performing risky operation: {operation}");
            await Task.Delay(200);
            
            if (operation == "fail")
            {
                throw new InvalidOperationException("Operation failed!");
            }
            
            Console.WriteLine("Operation succeeded!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw; // Will be caught by framework
        }
    })
    .AsCommand()
    .Done();

// Async route with cancellation support (simulated)
builder.Map("long-task {duration:int?}")
    .WithHandler(async (int? duration) =>
    {
        int totalSeconds = duration ?? 10;
        Console.WriteLine($"Starting long task ({totalSeconds} seconds)...");
        Console.WriteLine("Press Ctrl+C to cancel");
        
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nCancellation requested...");
        };
        
        try
        {
            for (int i = 1; i <= totalSeconds; i++)
            {
                await Task.Delay(1000, cts.Token);
                Console.WriteLine($"Progress: {i}/{totalSeconds} seconds");
            }
            Console.WriteLine("Task completed!");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Task was cancelled!");
        }
    })
    .AsCommand()
    .Done();

// Async route with multiple optional parameters (using optional options to avoid ambiguity)
builder.Map("deploy {service} --env? {environment} --version? {version}")
    .WithHandler(async (string service, string? environment, string? version) =>
    {
        environment ??= "production";
        version ??= "latest";
        
        Console.WriteLine($"Deploying {service} to {environment} (version: {version})...");
        await Task.Delay(2000);
        Console.WriteLine($"Successfully deployed {service} v{version} to {environment}");
    })
    .AsCommand()
    .Done();

// Help command
builder.Map("--help")
    .WithHandler(() =>
    {
        Console.WriteLine("Async Examples for TimeWarp.Nuru");
        Console.WriteLine("================================");
        Console.WriteLine("Commands:");
        Console.WriteLine("  ping                                    - Simple async command");
        Console.WriteLine("  fetch {url}                             - Async with required parameter");
        Console.WriteLine("  download {file} {destination?}          - Async with optional parameter");
        Console.WriteLine("  wait {seconds:int?}                     - Async with typed optional parameter");
        Console.WriteLine("  process {count:int}                     - Async returning exit code");
        Console.WriteLine("  risky {operation}                       - Async with error handling");
        Console.WriteLine("  long-task {duration:int?}               - Async with cancellation");
        Console.WriteLine("  deploy {service} --env? --version?      - Async with optional options");
    })
    .AsQuery()
    .Done();

// Build and run
NuruCoreApp app = builder.Build();
return await app.RunAsync(args);
