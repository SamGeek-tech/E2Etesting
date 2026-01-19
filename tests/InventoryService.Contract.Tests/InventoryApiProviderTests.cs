using System.Diagnostics;
using System.Net;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace InventoryService.Contract.Tests;

/// <summary>
/// Provider verification tests for InventoryService API.
/// Verifies that the API implementation matches the contracts defined by consumers.
/// </summary>
public class InventoryApiProviderTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private Process? _apiProcess;
    private readonly string _providerUri = "http://localhost:9003";

    public InventoryApiProviderTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Set default environment variables if not already set (for local testing)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL")))
        {
            Environment.SetEnvironmentVariable("PACT_BROKER_BASE_URL", "http://localhost:9292");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PACT_BROKER_USERNAME")))
        {
            Environment.SetEnvironmentVariable("PACT_BROKER_USERNAME", "admin");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PACT_BROKER_PASSWORD")))
        {
            Environment.SetEnvironmentVariable("PACT_BROKER_PASSWORD", "admin");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROVIDER_VERSION")))
        {
            Environment.SetEnvironmentVariable("PROVIDER_VERSION", "1.0.0");
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PUBLISH_VERIFICATION_RESULTS")))
        {
            Environment.SetEnvironmentVariable("PUBLISH_VERIFICATION_RESULTS", "true");
        }
        
        StartProvider();
    }

    private void StartProvider()
    {
        // Calculate path to the API project
        var projectPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "InventoryService.Api", "InventoryService.Api.csproj"));
        
        _output.WriteLine($"Starting Provider from: {projectPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls \"{_providerUri}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        
        // Set environment to Development to enable the ProviderStateMiddleware
        startInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development";
        // Use a test database
        startInfo.EnvironmentVariables["ConnectionStrings__DefaultConnection"] = "Data Source=test_inventory_provider.db";

        _apiProcess = new Process { StartInfo = startInfo };
        _apiProcess.OutputDataReceived += (sender, args) => 
        {
            if (args.Data != null) _output.WriteLine($"[Provider] {args.Data}");
        };
        _apiProcess.ErrorDataReceived += (sender, args) => 
        {
            if (args.Data != null) _output.WriteLine($"[Provider Error] {args.Data}");
        };
        
        _apiProcess.Start();
        _apiProcess.BeginOutputReadLine();
        _apiProcess.BeginErrorReadLine();

        // Wait for server to start (CI-safe). Don't rely on fixed sleeps.
        _output.WriteLine("Waiting for server to start...");
        WaitForProviderHealthy();
        _output.WriteLine("Server should be ready now.");
    }

    private void WaitForProviderHealthy()
    {
        // Inventory health endpoint: /api/inventory/health
        var healthUrl = $"{_providerUri}/api/inventory/health";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        var deadline = DateTime.UtcNow.AddSeconds(45);
        Exception? lastError = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = http.GetAsync(healthUrl).GetAwaiter().GetResult();
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            Thread.Sleep(500);
        }

        throw new TimeoutException($"Provider did not become healthy at {healthUrl} within timeout. Last error: {lastError?.Message}");
    }

    public void Dispose()
    {
        if (_apiProcess != null && !_apiProcess.HasExited)
        {
            _output.WriteLine("Stopping Provider...");
            _apiProcess.Kill(true);
            _apiProcess.Dispose();
        }
    }

    [Fact]
    public void EnsureInventoryApiHonorsPactWithConsumer()
    {
        // Get broker configuration from environment
        var pactBrokerUrlRaw = Environment.GetEnvironmentVariable("PACT_BROKER_BASE_URL");
        var pactBrokerTokenRaw = Environment.GetEnvironmentVariable("PACT_BROKER_TOKEN");
        var pactBrokerUsername = Environment.GetEnvironmentVariable("PACT_BROKER_USERNAME");
        var pactBrokerPassword = Environment.GetEnvironmentVariable("PACT_BROKER_PASSWORD");
        var providerVersion = Environment.GetEnvironmentVariable("PROVIDER_VERSION") ?? "1.0.0";
        var publishResults = Environment.GetEnvironmentVariable("PUBLISH_VERIFICATION_RESULTS")?.ToLower() == "true";
        
        _output.WriteLine("===========================================");
        _output.WriteLine("PACT PROVIDER VERIFICATION: InventoryService");
        _output.WriteLine("===========================================");
        _output.WriteLine($"Provider URI: {_providerUri}");
        _output.WriteLine($"Provider Version: {providerVersion}");
        _output.WriteLine($"Publish Results: {publishResults}");
        _output.WriteLine("");

        var config = new PactVerifierConfig
        {
            Outputters = new[] { new XunitOutput(_output) },
            LogLevel = PactLogLevel.Information
        };

        using var verifier = new PactVerifier("InventoryService", config);

        static string? NormalizeVariable(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            // CI variables sometimes leak through unexpanded like "$(VarName)"
            if (value.Contains("$(") && value.Contains(")"))
                return null;

            return value;
        }

        static string? NormalizeBrokerUrl(string? url)
        {
            url = NormalizeVariable(url);
            
            if (url == null) return null;

            // Accept host:port and normalize to http://host:port
            if (!url.Contains("://", StringComparison.Ordinal))
                url = "http://" + url;

            return url;
        }

        var pactBrokerUrl = NormalizeBrokerUrl(pactBrokerUrlRaw);
        
        _output.WriteLine($"DEBUG: Raw Token: '{pactBrokerTokenRaw}'");
        var pactBrokerToken = NormalizeVariable(pactBrokerTokenRaw);
        _output.WriteLine($"DEBUG: Normalized Token: '{pactBrokerToken}'");

        // Determine authentication method
        bool hasToken = !string.IsNullOrEmpty(pactBrokerToken);
        bool hasBasicAuth = !string.IsNullOrEmpty(pactBrokerUsername) && !string.IsNullOrEmpty(pactBrokerPassword);
        bool hasBroker = !string.IsNullOrEmpty(pactBrokerUrl) && (hasToken || hasBasicAuth);
        
        if (hasBroker && Uri.TryCreate(pactBrokerUrl, UriKind.Absolute, out var brokerUri))
        {
            // Fetch from Pact Broker (PactFlow or Self-Hosted)
            string brokerType = hasToken ? "PactFlow" : "Self-Hosted Broker";
            _output.WriteLine($"Fetching pacts from {brokerType}: {pactBrokerUrl}");
            _output.WriteLine($"Provider version: {providerVersion}");
            _output.WriteLine($"Authentication: {(hasToken ? "Token" : "Basic Auth")}");
            
            verifier
                .WithHttpEndpoint(new Uri(_providerUri))
                .WithPactBrokerSource(brokerUri, options =>
                {
                    // Configure authentication
                    if (hasToken)
                    {
                        options.TokenAuthentication(pactBrokerToken);
                    }
                    else
                    {
                        options.BasicAuthentication(pactBrokerUsername, pactBrokerPassword);
                    }
                    
                    options.ConsumerVersionSelectors(
                        new ConsumerVersionSelector { MainBranch = true }, // Latest from main
                        new ConsumerVersionSelector { DeployedOrReleased = true }, // All deployed versions
                        new ConsumerVersionSelector { Latest = true } // Also verify the absolute latest version (catches PRs)
                    );
                    options.EnablePending(); // Don't fail on new pacts
                    
                    if (publishResults)
                    {
                        options.PublishResults(providerVersion);
                    }
                })
                .WithProviderStateUrl(new Uri($"{_providerUri}/provider-states"))
                .Verify();
        }
        else
        {
            // Fallback to local file for local development
            if (!string.IsNullOrWhiteSpace(pactBrokerUrlRaw))
            {
                _output.WriteLine($"Broker URL is missing/invalid, falling back to local pact file. Raw value: '{pactBrokerUrlRaw}'");
            }
            else
            {
                _output.WriteLine("Broker credentials not found. Using local pact file for verification.");
            }
            _output.WriteLine("");
            _output.WriteLine("To use a broker, set:");
            _output.WriteLine("  - PactFlow: PACT_BROKER_BASE_URL and PACT_BROKER_TOKEN");
            _output.WriteLine("  - Self-Hosted: PACT_BROKER_BASE_URL, PACT_BROKER_USERNAME, and PACT_BROKER_PASSWORD");
            _output.WriteLine("");
            
            var pactDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!.FullName;
            string pactPath = Path.Combine(pactDir, "pacts", "InventoryClientSdk-InventoryService.json");

            if (!File.Exists(pactPath))
            {
                _output.WriteLine($"WARNING: Pact file not found at: {pactPath}");
                _output.WriteLine("Please run consumer tests first to generate the pact file.");
                throw new FileNotFoundException($"Pact file not found. Run consumer tests first.", pactPath);
            }

            _output.WriteLine($"Using local pact file: {pactPath}");
            
            verifier
                .WithHttpEndpoint(new Uri(_providerUri))
                .WithFileSource(new FileInfo(pactPath))
                .WithProviderStateUrl(new Uri($"{_providerUri}/provider-states"))
                .Verify();
        }
    }
}
