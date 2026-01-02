class ShopPage {
    constructor(page) {
        this.page = page;
    }

    async goto() {
        await this.page.goto('/Shop');
    }

    async placeOrder(productName) {
        const card = this.page.locator('.card').filter({ hasText: productName });
        await card.locator('.place-order-btn').click();
    }

    async setQuantity(productName, quantity) {
        const card = this.page.locator('.card').filter({ hasText: productName });
        await card.locator('input[name="quantity"]').fill(quantity.ToString ? quantity.ToString() : String(quantity));
    }

    async isOrderButtonEnabled(productName) {
        const card = this.page.locator('.card').filter({ hasText: productName });
        return await card.locator('.place-order-btn').isEnabled();
    }

    async getStockCount(productName) {
        const card = this.page.locator('.card').filter({ hasText: productName });
        const text = await card.locator("p:has-text('In Stock:')").textContent();
        return parseInt(text?.replace('In Stock: ', '').trim() || '0');
    }
}

module.exports = { ShopPage };
