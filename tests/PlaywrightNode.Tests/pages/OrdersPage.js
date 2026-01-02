class OrdersPage {
    constructor(page) {
        this.page = page;
    }

    async goto() {
        await this.page.goto('/Orders');
    }

    async isOrderVisible(productName) {
        const orderItem = this.page.locator('.order-item').filter({ hasText: productName }).first();
        return await orderItem.isVisible();
    }

    async getEmptyMessage() {
        return await this.page.locator("p:has-text('You haven\\'t placed any orders yet.')").textContent();
    }
}

module.exports = { OrdersPage };
