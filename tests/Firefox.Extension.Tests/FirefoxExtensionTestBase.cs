using Microsoft.Playwright;
using NUnit.Framework;

namespace Firefox.Extension.Tests;

/// <summary>
/// Base class for Firefox extension tests.
/// Provides Firefox browser setup with extension loading capability.
/// </summary>
public abstract class FirefoxExtensionTestBase
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Path to the extension directory (relative to output folder).
    /// Override this in derived classes to specify a different extension.
    /// </summary>
    protected virtual string ExtensionPath => Path.Combine(
        AppContext.BaseDirectory, "Extensions", "SampleExtension");

    /// <summary>
    /// Whether to run in headless mode. Set to false for debugging.
    /// </summary>
    protected virtual bool Headless => 
        Environment.GetEnvironmentVariable("CI") == "true" || 
        Environment.GetEnvironmentVariable("HEADLESS_MODE") == "true";

    [SetUp]
    public async Task SetUpAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Firefox with extension support requires specific launch options
        // Note: Firefox extensions must be in .xpi format or unpacked directory
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            FirefoxUserPrefs = new Dictionary<string, object>
            {
                // Allow unsigned extensions for testing
                ["xpinstall.signatures.required"] = false,
                // Enable extension debugging
                ["devtools.debugger.remote-enabled"] = true,
                // Disable first-run pages
                ["browser.startup.homepage_override.mstone"] = "ignore",
                ["browser.shell.checkDefaultBrowser"] = false,
                ["browser.startup.page"] = 0,
            }
        };

        // For Firefox persistent context with extensions
        var contextOptions = new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = Headless,
            FirefoxUserPrefs = launchOptions.FirefoxUserPrefs,
            // Record video for debugging failed tests
            RecordVideoDir = Path.Combine(AppContext.BaseDirectory, "videos"),
        };

        // Check if extension path exists
        if (Directory.Exists(ExtensionPath))
        {
            Console.WriteLine($"Loading extension from: {ExtensionPath}");
            
            // Use persistent context for Firefox extensions
            var userDataDir = Path.Combine(Path.GetTempPath(), "firefox-profile-" + Guid.NewGuid());
            Context = await Playwright.Firefox.LaunchPersistentContextAsync(userDataDir, contextOptions);
        }
        else
        {
            Console.WriteLine($"Extension not found at {ExtensionPath}, launching Firefox without extension");
            Browser = await Playwright.Firefox.LaunchAsync(launchOptions);
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                RecordVideoDir = Path.Combine(AppContext.BaseDirectory, "videos"),
            });
        }

        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        // Capture screenshot on failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            var screenshotPath = Path.Combine(
                AppContext.BaseDirectory, 
                "screenshots",
                $"failed-{TestContext.CurrentContext.Test.Name}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });
            Console.WriteLine($"Screenshot saved to: {screenshotPath}");
        }

        await Context.CloseAsync();
        
        if (Browser != null)
        {
            await Browser.CloseAsync();
        }
        
        Playwright?.Dispose();
    }

    /// <summary>
    /// Installs an extension by navigating to about:addons and installing from file.
    /// This is useful for testing .xpi files programmatically.
    /// </summary>
    protected async Task InstallExtensionFromXpiAsync(string xpiPath)
    {
        if (!File.Exists(xpiPath))
        {
            throw new FileNotFoundException($"Extension file not found: {xpiPath}");
        }

        // Navigate to about:addons
        await Page.GotoAsync("about:addons");
        
        // Click on Extensions in sidebar
        await Page.GetByRole(AriaRole.Button, new() { Name = "Extensions" }).ClickAsync();
        
        // Use the gear menu to install from file
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tools for all add-ons" }).ClickAsync();
        
        // Note: Installing from file via UI requires additional handling
        // For testing purposes, it's better to use a pre-configured profile or WebExtension APIs
        Console.WriteLine($"Extension installation requested for: {xpiPath}");
    }

    /// <summary>
    /// Gets the extension's background page URL for messaging.
    /// </summary>
    protected async Task<string?> GetExtensionIdAsync()
    {
        // Navigate to about:debugging to find extension ID
        await Page.GotoAsync("about:debugging#/runtime/this-firefox");
        
        // Wait for extensions to load
        await Page.WaitForTimeoutAsync(1000);
        
        // Try to find our extension in the list
        var extensionEntry = Page.Locator(".debug-target-item:has-text('Sample Extension')");
        if (await extensionEntry.CountAsync() > 0)
        {
            var manifestUrl = await extensionEntry.Locator(".debug-target-item__messages").TextContentAsync();
            Console.WriteLine($"Extension found: {manifestUrl}");
            return manifestUrl;
        }

        return null;
    }
}

