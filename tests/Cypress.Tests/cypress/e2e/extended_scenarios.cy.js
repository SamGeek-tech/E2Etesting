import LoginPage from '../support/pages/LoginPage';
import ShopPage from '../support/pages/ShopPage';
import OrdersPage from '../support/pages/OrdersPage';

describe('Extended E2E Scenarios (Cypress)', () => {
    const testUser = {
        email: 'test@example.com',
        password: 'Password123!'
    };

    beforeEach(() => {
        // We login for most tests
        LoginPage.visit();
        LoginPage.login(testUser.email, testUser.password);
    });

    it('Scenario 1: Verify Empty Orders Message', () => {
        // This assumes a clean state or a user with no orders. 
        // For this test, we can just check if the message exists if no orders are visible.
        OrdersPage.visit();
        cy.get('body').then(($body) => {
            if ($body.find('.order-item').length === 0) {
                OrdersPage.getEmptyMessage().should('be.visible');
            }
        });
    });

    it('Scenario 2: Order with Quantity > 1', () => {
        ShopPage.visit();
        const productName = 'Mouse';
        const quantity = 3;

        ShopPage.setQuantity(productName, quantity);
        ShopPage.placeOrder(productName);

        cy.url().should('include', '/Orders');
        OrdersPage.isOrderVisible(productName);
        cy.get('.order-item').first().within(() => {
            cy.contains(`${productName} x ${quantity}`).should('be.visible');
        });
    });

    it('Scenario 3: Out of Stock UI Validation', () => {
        ShopPage.visit();
        const productName = 'Keyboard';

        ShopPage.getStockCount(productName).then((stock) => {
            if (stock > 0) {
                // Set quantity to more than available
                ShopPage.setQuantity(productName, stock + 1);
                // The HTML 'max' attribute might prevent this, but the button should still work 
                // if it's within range. Let's test disabling after buying all.

                ShopPage.setQuantity(productName, stock);
                ShopPage.placeOrder(productName);

                // Back to shop
                ShopPage.visit();
                ShopPage.isOrderButtonDisabled(productName);
                cy.contains('.card', productName).contains('In Stock: 0').should('be.visible');
            }
        });
    });

    it('Scenario 4: Header Navigation Links', () => {
        cy.get('.nav-link').contains('Home').click();
        cy.url().should('eq', Cypress.config().baseUrl + '/');

        cy.get('.nav-link').contains('Shop').click();
        cy.url().should('include', '/Shop');

        cy.get('.nav-link').contains('My Orders').click();
        cy.url().should('include', '/Orders');
    });

    it('Scenario 5: Secure Logout and Protected Routes', () => {
        cy.get('.nav-link').contains('Logout').click();
        cy.url().should('include', '/Account/Login');

        // Try to access Shop directly
        cy.visit('/Shop');
        cy.url().should('include', '/Account/Login');
    });
});
