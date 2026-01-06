// ***********************************************************
// This example support/e2e.js is processed and
// loaded automatically before your test files.
//
// This is a great place to put global configuration and
// behavior that modifies Cypress.
//
// You can change the location of this file or turn off
// automatically serving support files with the
// 'supportFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/configuration
// ***********************************************************

// Import commands.js using ES2015 syntax:
import './commands'

// Global setup: Replenish inventory stock before all tests
before(() => {
    const inventoryUrl = Cypress.env('INVENTORY_URL') || 'http://127.0.0.1:5001';
    
    const products = [
        { id: 1, stockQuantity: 100 },
        { id: 2, stockQuantity: 100 },
        { id: 3, stockQuantity: 100 },
    ];

    cy.log('🔧 Replenishing inventory stock...');
    
    products.forEach((product) => {
        cy.request({
            method: 'PUT',
            url: `${inventoryUrl}/api/inventory/${product.id}`,
            body: {
                id: product.id,
                stockQuantity: product.stockQuantity
            },
            failOnStatusCode: false
        }).then((response) => {
            if (response.status === 200) {
                cy.log(`✅ Product ${product.id}: Stock replenished`);
            } else {
                cy.log(`⚠️ Product ${product.id}: Failed (${response.status})`);
            }
        });
    });
});
