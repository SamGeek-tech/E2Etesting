import LoginPage from '../support/pages/LoginPage';
import ShopPage from '../support/pages/ShopPage';
import OrdersPage from '../support/pages/OrdersPage';

describe('Order Flow Journey (Cypress Comparison)', () => {
    const testUser = {
        email: 'test@example.com',
        password: 'Password123!'
    };

    beforeEach(() => {
        // Clear cookies/localStorage if needed
    });

    it('Full User Journey: Place Order & Verify History', () => {
        // 1. Act: Login
        LoginPage.visit();
        LoginPage.login(testUser.email, testUser.password);

        // Assert: Redirect to Home
        cy.url().should('eq', Cypress.config().baseUrl + '/');

        // 2. Act: Browse and Order Laptop
        ShopPage.visit();

        // Capture initial stock
        cy.contains('.card', 'Laptop').find('p').contains('In Stock:').then($p => {
            const initialStock = parseInt($p.text().replace('In Stock: ', '').trim());

            ShopPage.placeOrder('Laptop');

            // Assert: Redirect to Orders
            cy.url().should('include', '/Orders');

            // 3. Assert: Order exists in history
            OrdersPage.isOrderVisible('Laptop');

            // 4. Assert: Stock depleted
            ShopPage.visit();
            cy.contains('.card', 'Laptop').find('p').contains('In Stock:').should($p => {
                const finalStock = parseInt($p.text().replace('In Stock: ', '').trim());
                expect(finalStock).to.eq(initialStock - 1);
            });
        });
    });

    it('Invalid Login: Shows Error', () => {
        LoginPage.visit();
        LoginPage.login('wrong@example.com', 'WrongPass');

        LoginPage.getErrorMessage()
            .should('be.visible')
            .and('contain', 'Invalid login attempt');
    });
});
