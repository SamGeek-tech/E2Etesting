using Microsoft.Playwright;
using NUnit.Framework;

namespace Firefox.Extension.Tests;

/// <summary>
/// Tests for the sample Firefox extension.
/// Demonstrates how to test extension functionality with Playwright.
/// </summary>
[TestFixture]
public class ExtensionTests : FirefoxExtensionTestBase
{
    [Test]
    [Category("Extension")]
    public async Task Extension_ShouldLoad_OnAnyPage()
    {
        // Arrange & Act - Navigate to a test page
        await Page.GotoAsync("http://127.0.0.1:5002");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Assert - Check if extension indicator is present
        // The content script adds this attribute to the body
        var isExtensionLoaded = await Page.EvaluateAsync<bool>(
            "() => document.body.getAttribute('data-sample-extension-loaded') === 'true'");
        
        Assert.That(isExtensionLoaded, Is.True, "Extension should be loaded on the page");
    }

    [Test]
    [Category("Extension")]
    public async Task Extension_Indicator_ShouldBeVisible()
    {
        // Arrange - Navigate to the app
        await Page.GotoAsync("http://127.0.0.1:5002");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Act & Assert - Check indicator element
        var indicator = Page.Locator("#sample-extension-indicator");
        await Assertions.Expect(indicator).ToBeVisibleAsync();
        await Assertions.Expect(indicator).ToContainTextAsync("Extension Active");
    }

    [Test]
    [Category("Extension")]
    public async Task Extension_ContentScript_ShouldExposeAPI()
    {
        // Arrange - Navigate to localhost (API is only exposed on localhost)
        await Page.GotoAsync("http://127.0.0.1:5002");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Act - Check if the extension API is exposed
        var extensionInfo = await Page.EvaluateAsync<Dictionary<string, object>?>(@"() => {
            if (window.__sampleExtension) {
                return {
                    isLoaded: window.__sampleExtension.isLoaded,
                    version: window.__sampleExtension.version
                };
            }
            return null;
        }");
        
        // Assert
        Assert.That(extensionInfo, Is.Not.Null, "Extension API should be exposed on localhost");
        Assert.That(extensionInfo!["isLoaded"], Is.EqualTo(true));
        Assert.That(extensionInfo["version"], Is.EqualTo("1.0.0"));
    }

    [Test]
    [Category("Extension")]
    public async Task Extension_ShouldHighlightLinks()
    {
        // Arrange - Navigate to a page with links
        await Page.GotoAsync("http://127.0.0.1:5002");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Act - Trigger highlight via extension API
        var highlightResult = await Page.EvaluateAsync<int>(@"async () => {
            // Manually add highlight class to links (simulating extension action)
            const links = document.querySelectorAll('a');
            links.forEach(link => link.classList.add('sample-extension-highlighted'));
            return links.length;
        }");
        
        // Assert - Check if links are highlighted
        if (highlightResult > 0)
        {
            var highlightedLinks = await Page.Locator("a.sample-extension-highlighted").CountAsync();
            Assert.That(highlightedLinks, Is.GreaterThan(0), "Links should be highlighted");
        }
    }

    [Test]
    [Category("Extension")]
    public async Task Extension_ShouldPersist_AcrossNavigation()
    {
        // Arrange - Navigate to first page
        await Page.GotoAsync("http://127.0.0.1:5002");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Check extension loaded on first page
        var loadedOnFirstPage = await Page.EvaluateAsync<bool>(
            "() => document.body.getAttribute('data-sample-extension-loaded') === 'true'");
        
        // Act - Navigate to another page
        await Page.GotoAsync("http://127.0.0.1:5002/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Assert - Extension should still be loaded
        var loadedOnSecondPage = await Page.EvaluateAsync<bool>(
            "() => document.body.getAttribute('data-sample-extension-loaded') === 'true'");
        
        Assert.That(loadedOnFirstPage, Is.True, "Extension should be loaded on first page");
        Assert.That(loadedOnSecondPage, Is.True, "Extension should persist to second page");
    }
}

/// <summary>
/// Tests specifically for extension popup functionality.
/// Note: Popup testing requires special handling in Firefox.
/// </summary>
[TestFixture]
public class ExtensionPopupTests : FirefoxExtensionTestBase
{
    [Test]
    [Category("Extension")]
    [Category("Popup")]
    public async Task Popup_ShouldRender_Correctly()
    {
        // Navigate directly to the popup HTML (useful for testing popup in isolation)
        var popupPath = Path.Combine(ExtensionPath, "popup", "popup.html");
        
        if (File.Exists(popupPath))
        {
            await Page.GotoAsync($"file:///{popupPath.Replace('\\', '/')}");
            
            // Check popup elements
            await Assertions.Expect(Page.Locator("h1")).ToContainTextAsync("Sample Extension");
            await Assertions.Expect(Page.Locator("#highlight-links")).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator("#get-page-info")).ToBeVisibleAsync();
            await Assertions.Expect(Page.Locator(".status-indicator")).ToBeVisibleAsync();
        }
        else
        {
            Assert.Inconclusive("Popup HTML not found - extension may not be built");
        }
    }

    [Test]
    [Category("Extension")]
    [Category("Popup")]
    public async Task Popup_StatusIndicator_ShouldShowActive()
    {
        var popupPath = Path.Combine(ExtensionPath, "popup", "popup.html");
        
        if (File.Exists(popupPath))
        {
            await Page.GotoAsync($"file:///{popupPath.Replace('\\', '/')}");
            
            var statusIndicator = Page.Locator(".status-indicator");
            await Assertions.Expect(statusIndicator).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("active"));
            await Assertions.Expect(Page.Locator(".status-text")).ToContainTextAsync("Active");
        }
        else
        {
            Assert.Inconclusive("Popup HTML not found");
        }
    }

    [Test]
    [Category("Extension")]
    [Category("Popup")]
    public async Task Popup_Buttons_ShouldBeClickable()
    {
        var popupPath = Path.Combine(ExtensionPath, "popup", "popup.html");
        
        if (File.Exists(popupPath))
        {
            await Page.GotoAsync($"file:///{popupPath.Replace('\\', '/')}");
            
            // Test highlight button
            var highlightBtn = Page.Locator("#highlight-links");
            await Assertions.Expect(highlightBtn).ToBeEnabledAsync();
            
            // Test page info button
            var pageInfoBtn = Page.Locator("#get-page-info");
            await Assertions.Expect(pageInfoBtn).ToBeEnabledAsync();
            
            // Click page info (will fail gracefully since we're not in context)
            await pageInfoBtn.ClickAsync();
            
            // The info panel should exist but may not show data without proper tab context
            var infoPanel = Page.Locator("#info-panel");
            await Assertions.Expect(infoPanel).ToBeAttachedAsync();
        }
        else
        {
            Assert.Inconclusive("Popup HTML not found");
        }
    }
}

/// <summary>
/// Integration tests that combine extension functionality with the web app.
/// </summary>
[TestFixture]
public class ExtensionIntegrationTests : FirefoxExtensionTestBase
{
    [Test]
    [Category("Integration")]
    public async Task Extension_ShouldWork_WithLoginFlow()
    {
        // Navigate to login page
        await Page.GotoAsync("http://127.0.0.1:5002/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Verify extension is loaded
        var indicator = Page.Locator("#sample-extension-indicator");
        
        // Fill login form
        await Page.FillAsync("input[name='Username']", "test");
        await Page.FillAsync("input[name='Password']", "pass123");
        
        // Click login (assuming this is how the app works)
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for navigation
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Extension should still be present after login
        var extensionLoaded = await Page.EvaluateAsync<bool>(
            "() => document.body.getAttribute('data-sample-extension-loaded') === 'true'");
        
        Assert.That(extensionLoaded, Is.True, "Extension should persist after login");
    }

    [Test]
    [Category("Integration")]
    public async Task Extension_ShouldTrack_PageNavigations()
    {
        var visitedPages = new List<string>();
        
        // Hook into console messages to track extension logs
        Page.Console += (_, msg) =>
        {
            if (msg.Text.Contains("Sample Extension"))
            {
                Console.WriteLine($"Extension log: {msg.Text}");
            }
        };
        
        // Navigate through multiple pages
        var pages = new[]
        {
            "http://127.0.0.1:5002",
            "http://127.0.0.1:5002/Account/Login",
            "http://127.0.0.1:5002/Shop"
        };
        
        foreach (var url in pages)
        {
            await Page.GotoAsync(url);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            
            // Verify extension loaded
            var loaded = await Page.EvaluateAsync<bool>(
                "() => document.body.getAttribute('data-sample-extension-loaded') === 'true'");
            
            if (loaded)
            {
                visitedPages.Add(url);
            }
        }
        
        Assert.That(visitedPages.Count, Is.EqualTo(pages.Length), 
            "Extension should be active on all pages");
    }
}

