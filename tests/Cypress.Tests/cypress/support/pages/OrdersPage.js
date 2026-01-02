class OrdersPage {
    visit() {
        cy.visit('/Orders');
    }

    isOrderVisible(productName) {
        return cy.get('.order-item').contains(productName).should('be.visible');
    }

    getEmptyMessage() {
        return cy.get('p').contains("You haven't placed any orders yet.");
    }
}

export default new OrdersPage();
