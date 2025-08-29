#!/usr/bin/dotnet --
#:property LangVersion=preview
#:property EnablePreviewFeatures=true

using System.Diagnostics;
using System.Text.Json;

WriteLine("Testing TimeWarp.Nuru.Mcp Server...\n");

// Start the MCP server process
string mcpPath = Path.GetFullPath(Path.Combine("..", "..", "Source", "TimeWarp.Nuru.Mcp"));
WriteLine($"Starting MCP server from: {mcpPath}");

ProcessStartInfo psi = new()
{
    FileName = "dotnet",
    Arguments = $"run --project \"{mcpPath}\" -c Release",
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};

using var process = Process.Start(psi);
if (process is null)
{
    WriteLine("❌ Failed to start MCP server");
    return 1;
}

try
{
    // Give the server a moment to start
    await Task.Delay(1000);

    WriteLine("Sending initialize request...");

    // Send initialize request
    string initRequest = """
    {"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"1.0.0","clientInfo":{"name":"test-client","version":"1.0.0"}},"id":1}
    """;

    await process.StandardInput.WriteLineAsync(initRequest);
    await process.StandardInput.FlushAsync();

    // Read response with timeout
    string? response = null;
    using (CancellationTokenSource cts = new(TimeSpan.FromSeconds(5)))
    {
        Task<string?> readTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

        try
        {
            response = await readTask.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            WriteLine("⚠️ Timeout waiting for response");
        }
    }

    if (response is not null)
    {
        WriteLine("\n✅ Got response from server!");
        WriteLine("\nResponse (raw):");
        WriteLine(response);

        try
        {
            var json = JsonDocument.Parse(response);

            // Check for successful initialization
            if (json.RootElement.TryGetProperty("result", out JsonElement result))
            {
                WriteLine("\n✅ Server initialized successfully!");

                // Even if tools object exists (even if empty), let's try listing tools

                // First, let's list the available tools
                WriteLine("\n" + new string('-', 50));
                WriteLine("\nTest 2: Listing available tools...");

                string listToolsRequest = """
                {"jsonrpc":"2.0","method":"tools/list","id":2}
                """;

                await process.StandardInput.WriteLineAsync(listToolsRequest);
                await process.StandardInput.FlushAsync();

                // Read tool response
                using (CancellationTokenSource toolCts = new(TimeSpan.FromSeconds(5)))
                {
                    Task<string?> toolReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                    try
                    {
                        string? toolResponse = await toolReadTask.WaitAsync(toolCts.Token);
                        if (toolResponse is not null)
                    {
                        WriteLine("\n✅ Got tools/list response!");
                        WriteLine("\nTools List Response:");
                        WriteLine(toolResponse);

                        // Test 3: Call get_random_number tool
                        WriteLine("\n" + new string('-', 50));
                        WriteLine("\nTest 3: Calling get_random_number tool...");

                        string callToolRequest = """
                        {"jsonrpc":"2.0","method":"tools/call","params":{"name":"get_random_number","arguments":{"min":10,"max":50}},"id":3}
                        """;

                        await process.StandardInput.WriteLineAsync(callToolRequest);
                        await process.StandardInput.FlushAsync();

                        // Read the tool execution response
                        using (CancellationTokenSource callCts = new(TimeSpan.FromSeconds(5)))
                        {
                            Task<string?> callReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                            try
                            {
                                string? callResponse = await callReadTask.WaitAsync(callCts.Token);
                                if (callResponse is not null)
                                {
                                    WriteLine("\n✅ Got tool execution response!");
                                    WriteLine("\nTool Execution Response:");
                                    WriteLine(callResponse);

                                    // Parse to verify it has a result
                                    try
                                    {
                                        var callJson = JsonDocument.Parse(callResponse);
                                        if (callJson.RootElement.TryGetProperty("result", out JsonElement callResult))
                                        {
                                            WriteLine("\n✅ Tool executed successfully! Random number generated.");
                                        }
                                    }
                                    catch { }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                WriteLine("⚠️ Timeout waiting for tool execution response");
                            }
                        }

                        // Test 4: Call list_examples tool
                        WriteLine("\n" + new string('-', 50));
                        WriteLine("\nTest 4: Calling list_examples tool...");

                        string listExamplesRequest = """
                        {"jsonrpc":"2.0","method":"tools/call","params":{"name":"list_examples","arguments":{}},"id":4}
                        """;

                        await process.StandardInput.WriteLineAsync(listExamplesRequest);
                        await process.StandardInput.FlushAsync();

                        using (CancellationTokenSource listCts = new(TimeSpan.FromSeconds(5)))
                        {
                            Task<string?> listReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                            try
                            {
                                string? listResponse = await listReadTask.WaitAsync(listCts.Token);
                                if (listResponse is not null)
                                {
                                    WriteLine("\n✅ Got list_examples response!");
                                    WriteLine("\nList Examples Response (truncated):");
                                    // Truncate for readability
                                    if (listResponse.Length > 200)
                                        WriteLine(string.Concat(listResponse.AsSpan(0, 200), "..."));
                                    else
                                        WriteLine(listResponse);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                WriteLine("⚠️ Timeout waiting for list examples response");
                            }
                        }

                        // Test 5: Call get_example with specific example
                        WriteLine("\n" + new string('-', 50));
                        WriteLine("\nTest 5: Getting 'basic' example...");

                        string getExampleRequest = """
                        {"jsonrpc":"2.0","method":"tools/call","params":{"name":"get_example","arguments":{"name":"basic"}},"id":5}
                        """;

                        await process.StandardInput.WriteLineAsync(getExampleRequest);
                        await process.StandardInput.FlushAsync();

                        using (CancellationTokenSource getCts = new(TimeSpan.FromSeconds(5)))
                        {
                            Task<string?> getReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                            try
                            {
                                string? getResponse = await getReadTask.WaitAsync(getCts.Token);
                                if (getResponse is not null)
                                {
                                    WriteLine("\n✅ Got get_example response!");
                                    WriteLine("\nGet Example Response (truncated):");
                                    // Truncate for readability
                                    if (getResponse.Length > 300)
                                        WriteLine(string.Concat(getResponse.AsSpan(0, 300), "..."));
                                    else
                                        WriteLine(getResponse);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                WriteLine("⚠️ Timeout waiting for get example response");
                            }
                        }

                        // Test 6: Check cache status
                        WriteLine("\n" + new string('-', 50));
                        WriteLine("\nTest 6: Checking cache status...");

                        string cacheStatusRequest = """
                        {"jsonrpc":"2.0","method":"tools/call","params":{"name":"cache_status","arguments":{}},"id":6}
                        """;

                        await process.StandardInput.WriteLineAsync(cacheStatusRequest);
                        await process.StandardInput.FlushAsync();

                        using (CancellationTokenSource statusCts = new(TimeSpan.FromSeconds(5)))
                        {
                            Task<string?> statusReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                            try
                            {
                                string? statusResponse = await statusReadTask.WaitAsync(statusCts.Token);
                                if (statusResponse is not null)
                                {
                                    WriteLine("\n✅ Got cache_status response!");
                                    WriteLine("\nCache Status Response:");
                                    // Parse and display just the text content
                                    try
                                    {
                                        var statusJson = JsonDocument.Parse(statusResponse);
                                        if (statusJson.RootElement.TryGetProperty("result", out JsonElement statusResult) &&
                                            statusResult.TryGetProperty("content", out JsonElement statusContent))
                                        {
                                            foreach (JsonElement item in statusContent.EnumerateArray())
                                            {
                                                if (item.TryGetProperty("text", out JsonElement text))
                                                {
                                                    WriteLine(text.GetString());
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        WriteLine(statusResponse);
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                WriteLine("⚠️ Timeout waiting for cache status response");
                            }
                        }

                        // Test 7: Clear cache
                        WriteLine("\n" + new string('-', 50));
                        WriteLine("\nTest 7: Clearing cache...");

                        string clearCacheRequest = """
                        {"jsonrpc":"2.0","method":"tools/call","params":{"name":"clear_cache","arguments":{}},"id":7}
                        """;

                        await process.StandardInput.WriteLineAsync(clearCacheRequest);
                        await process.StandardInput.FlushAsync();

                        using (CancellationTokenSource clearCts = new(TimeSpan.FromSeconds(5)))
                        {
                            Task<string?> clearReadTask = Task.Run(() => process.StandardOutput.ReadLineAsync());

                            try
                            {
                                string? clearResponse = await clearReadTask.WaitAsync(clearCts.Token);
                                if (clearResponse is not null)
                                {
                                    WriteLine("\n✅ Got clear_cache response!");
                                    WriteLine("\nClear Cache Response:");
                                    // Parse and display just the text content
                                    try
                                    {
                                        var clearJson = JsonDocument.Parse(clearResponse);
                                        if (clearJson.RootElement.TryGetProperty("result", out JsonElement clearResult) &&
                                            clearResult.TryGetProperty("content", out JsonElement clearContent))
                                        {
                                            foreach (JsonElement item in clearContent.EnumerateArray())
                                            {
                                                if (item.TryGetProperty("text", out JsonElement text))
                                                {
                                                    WriteLine(text.GetString());
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        WriteLine(clearResponse);
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                WriteLine("⚠️ Timeout waiting for clear cache response");
                            }
                        }
                    }
                    }
                    catch (OperationCanceledException)
                    {
                        WriteLine("⚠️ Timeout waiting for tool response");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine($"⚠️ Could not parse response as JSON: {ex.Message}");
        }
    }
    else
    {
        WriteLine("❌ No response received from server");

        // Check for errors
        string errorOutput = await process.StandardError.ReadToEndAsync();
        if (!string.IsNullOrEmpty(errorOutput))
        {
            WriteLine("\nError output:");
            WriteLine(errorOutput);
        }
    }
}
finally
{
    // Clean up - MCP spec says to close stdin and wait, but we'll just kill it
    WriteLine("\n" + new string('=', 50));
    WriteLine("Shutting down MCP server...");

    if (!process.HasExited)
    {
        try
        {
            // Close stdin (this should signal the server to exit per MCP spec)
            process.StandardInput.Close();

            // Give it 2 seconds to exit gracefully
            Task exitTask = process.WaitForExitAsync();
            if (await Task.WhenAny(exitTask, Task.Delay(2000)) == exitTask)
            {
                WriteLine("✅ Server exited after stdin closed");
            }
            else
            {
                WriteLine("⚠️ Server didn't exit, forcing shutdown...");
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
        catch
        {
            process.Kill();
            await process.WaitForExitAsync();
        }
    }

    WriteLine("✅ Test complete!");
}

WriteLine("\nIf the tests passed, you can configure Claude with:");
WriteLine($@"{{
  ""servers"": {{
    ""timewarp-nuru"": {{
      ""type"": ""stdio"",
      ""command"": ""dotnet"",
      ""args"": [
        ""run"",
        ""--project"",
        ""{mcpPath}""
      ]
    }}
  }}
}}");

return 0;