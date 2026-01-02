class ShopPage {
    visit() {
        cy.visit('/Shop');
    }

    placeOrder(productName) {
        cy.contains('.card', productName).within(() => {
            cy.get('.place-order-btn').click();
        });
    }

    setQuantity(productName, quantity) {
        cy.contains('.card', productName).within(() => {
            cy.get('input[name="quantity"]').clear().type(quantity);
        });
    }

    isOrderButtonEnabled(productName) {
        return cy.contains('.card', productName).find('.place-order-btn').should('not.be.disabled');
    }

    isOrderButtonDisabled(productName) {
        return cy.contains('.card', productName).find('.place-order-btn').should('be.disabled');
    }

    getStockCount(productName) {
        return cy.contains('.card', productName).find('p').contains('In Stock:').then($p => {
            const text = $p.text();
            return parseInt(text.replace('In Stock: ', '').trim());
        });
    }
}

export default new ShopPage();
