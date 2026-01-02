# E2E Framework Comparison: Playwright vs. Cypress vs. Selenium

This document compares the implementation of the same 10 E2E test scenarios across four different framework variations.

## 1. Tooling & Setup

| Feature | Playwright (.NET) | Playwright (Node.js) | Cypress (JS/Node) | Selenium (.NET) |
| :--- | :--- | :--- | :--- | :--- |
| **Language** | C# / .NET 8+ | JavaScript / TypeScript | JavaScript / TypeScript | C# / .NET 8+ |
| **Test Runner** | NUnit / xUnit | Playwright Runner | Cypress Runner (Mocha) | NUnit / xUnit / MSTest |
| **Driver** | Built-in (CDP) | Built-in (CDP) | Internal (In-process) | WebDriver (External) |
| **Installation** | NuGet + CLI | NPM Package | NPM Package | NuGet + Driver Binaries |
| **Auto-waiting** | Native | Native | Native | Manual (WebDriverWait) |

## 2. Framework Architecture

### Playwright (Both)
- **Modern**: Uses Chrome DevTools Protocol (CDP) for direct browser control.
- **Speed**: Very fast execution and startup.
- **Reliability**: Excellent auto-waiting logic; virtually zero "sleep" commands needed.
- **Multi-Everything**: Native support for multiple tabs, frames, and contexts.

### Cypress (JS)
- **Developer Experience**: Best-in-class UI for debugging and "time-travel".
- **Execution**: Runs *inside* the browser context alongside your application.
- **Limitations**: Single-tab only; struggles with certain complex auth/origin scenarios.

### Selenium (.NET)
- **Industry Standard**: The oldest and most widely used framework.
- **Compatibility**: Supports every browser imaginable (ancient to modern).
- **Control**: Very granular control over the browser via the W3C WebDriver standard.
- **Maintenance**: Requires more "boilerplate" code for waiting (ExpectedConditions) and driver management.

## 3. Code Comparison (Login Method)

### Playwright (.NET)
```csharp
public async Task LoginAsync(string email, string password) {
    await EmailInput.FillAsync(email);
    await PasswordInput.FillAsync(password);
    await LoginButton.ClickAsync();
}
```

### Playwright (Node.js)
```javascript
async login(email, password) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
}
```

### Cypress (JS)
```javascript
login(email, password) {
    cy.get('#email').type(email);
    cy.get('#password').type(password);
    cy.get('#login-button').click();
}
```

### Selenium (.NET)
```csharp
public void Login(string email, string password) {
    EmailInput.SendKeys(email);
    PasswordInput.SendKeys(password);
    LoginButton.Click();
}
```

## 4. Summary Table

| Metric | Winner | Rationale |
| :--- | :--- | :--- |
| **Performance** | **Playwright (Node)** | Lowest overhead and native asynchronous speed. |
| **Debugging** | **Cypress** | Visual runner is unmatched for troubleshooting UI issues. |
| **Stability** | **Playwright** | Auto-waiting eliminates most transient flakiness. |
| **Legacy Support** | **Selenium** | Only choice for very old browsers or legacy infrastructure. |

## 5. Conclusion
- **Use Playwright** if you want speed, modern features, and stability.
- **Use Cypress** if you prioritize developer happiness and easy debugging of frontend code.
- **Use Selenium** if you have existing legacy test suites or need to test on extremely niche browser/OS combinations.
