class LoginPage {
    constructor(page) {
        this.page = page;
        this.emailInput = page.locator('#email');
        this.passwordInput = page.locator('#password');
        this.loginButton = page.locator('#login-button');
        this.errorSummary = page.locator('.text-danger');
    }

    async goto() {
        await this.page.goto('/Account/Login');
    }

    async login(email, password) {
        await this.emailInput.fill(email);
        await this.passwordInput.fill(password);
        await this.loginButton.click();
    }

    async getErrorMessage() {
        return await this.errorSummary.textContent();
    }
}

module.exports = { LoginPage };
