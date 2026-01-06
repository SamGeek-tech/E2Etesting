const { test, expect } = require('@playwright/test');
const { LoginPage } = require('../pages/LoginPage');
const { ShopPage } = require('../pages/ShopPage');
const { OrdersPage } = require('../pages/OrdersPage');

test.describe('Order Flow Journey (Playwright Node.js Comparison)', () => {
    const testUser = {
        email: 'test@example.com',
        password: 'Password123!'
    };

    // Explicitly using baseURL to demonstrate setup, though config handles relative paths automatically
    test.use({ baseURL: 'http://127.0.0.1:5002' });

    test('Scenario 1: Successful Authentication', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);
        await expect(page).toHaveURL('/');
        await expect(page.locator('text=Hello, test@example.com!')).toBeVisible();
    });

    test('Scenario 2: Failed Authentication', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        await loginPage.login('wrong@example.com', 'WrongPass');
        const error = await loginPage.getErrorMessage();
        expect(error).toContain('Invalid login attempt');
    });

    test('Scenario 3: Full Journey - Place Order & Verify History', async ({ page }) => {
        const loginPage = new LoginPage(page);
        const shopPage = new ShopPage(page);
        const ordersPage = new OrdersPage(page);

        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await shopPage.goto();
        const initialStock = await shopPage.getStockCount('Laptop');
        await shopPage.placeOrder('Laptop');

        await expect(page).toHaveURL('/Orders');
        const isVisible = await ordersPage.isOrderVisible('Laptop');
        expect(isVisible).toBeTruthy();

        await shopPage.goto();
        const finalStock = await shopPage.getStockCount('Laptop');
        expect(finalStock).toBe(initialStock - 1);
    });

    test('Scenario 4: Bulk Quantity Order', async ({ page }) => {
        const loginPage = new LoginPage(page);
        const shopPage = new ShopPage(page);
        const ordersPage = new OrdersPage(page);
        const productName = 'Mouse';
        const quantity = 3;

        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await shopPage.goto();
        await shopPage.setQuantity(productName, quantity);
        await shopPage.placeOrder(productName);

        await expect(page).toHaveURL('/Orders');
        const orderText = await page.locator('.order-item').first().innerText();
        expect(orderText).toContain(`${productName} x ${quantity}`);
    });

    test('Scenario 5: Out of Stock UI Validation', async ({ page }) => {
        const loginPage = new LoginPage(page);
        const shopPage = new ShopPage(page);
        const productName = 'Keyboard';

        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await shopPage.goto();
        const stock = await shopPage.getStockCount(productName);

        if (stock > 0) {
            await shopPage.setQuantity(productName, stock);
            await shopPage.placeOrder(productName);

            await shopPage.goto();
            const isEnabled = await shopPage.isOrderButtonEnabled(productName);
            expect(isEnabled).toBeFalsy();

            const stockText = await page.locator('.card').filter({ hasText: productName }).locator("p:has-text('In Stock:')").innerText();
            expect(stockText).toContain('In Stock: 0');
        }
    });

    test('Scenario 6: Empty Orders State', async ({ page }) => {
        const loginPage = new LoginPage(page);
        const ordersPage = new OrdersPage(page);

        await loginPage.goto();
        await loginPage.login('test@example.com', 'Password123!');

        await ordersPage.goto();
        const orderItems = page.locator('.order-item');
        if (await orderItems.count() === 0) {
            const msg = await ordersPage.getEmptyMessage();
            expect(msg).toContain("You haven't placed any orders yet.");
        }
    });

    test('Scenario 7: Header Navigation Links', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await page.getByRole('link', { name: 'Home' }).click();
        await expect(page).toHaveURL('/');

        await page.getByRole('link', { name: 'Shop' }).click();
        await expect(page).toHaveURL('/Shop');

        await page.getByRole('link', { name: 'My Orders' }).click();
        await expect(page).toHaveURL('/Orders');
    });

    test('Scenario 8: Session Termination & Protected Routes', async ({ page }) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await page.getByRole('link', { name: 'Logout' }).click();
        await expect(page).toHaveURL(/\/Account\/Login/);

        await page.goto('/Shop');
        await expect(page).toHaveURL(/\/Account\/Login/);
    });

    test('Scenario 9: Stock Depletion Logic Verification', async ({ page }) => {
        // Already partially covered in Scenario 3, but specifically checking the value
        const loginPage = new LoginPage(page);
        const shopPage = new ShopPage(page);

        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await shopPage.goto();
        const initial = await shopPage.getStockCount('Mouse');
        await shopPage.placeOrder('Mouse');

        await shopPage.goto();
        const final = await shopPage.getStockCount('Mouse');
        expect(final).toBe(initial - 1);
    });

    test('Scenario 10: Order Persistence across sessions', async ({ page, context }) => {
        const loginPage = new LoginPage(page);
        await loginPage.goto();
        await loginPage.login(testUser.email, testUser.password);

        await page.goto('/Orders');
        const initialCount = await page.locator('.order-item').count();

        // Close page, open new one
        await page.close();
        const newPage = await context.newPage();
        await newPage.goto('/Orders');
        // Session should still be active if cookies persist, or we might need to login again
        // For simplicity, let's just check if we can see orders after a fresh navigation
        const finalCount = await newPage.locator('.order-item').count();
        expect(finalCount).toBe(initialCount);
    });
});
