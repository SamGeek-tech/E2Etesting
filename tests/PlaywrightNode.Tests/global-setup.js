/**
 * Global setup for Playwright tests.
 * Replenishes inventory stock before running tests.
 */
const INVENTORY_API_URL = process.env.INVENTORY_URL || 'http://127.0.0.1:5001';

async function globalSetup() {
    console.log('🔧 Global Setup: Replenishing inventory stock...');
    
    const products = [
        { id: 1, name: 'Laptop', stockQuantity: 100 },
        { id: 2, name: 'Mouse', stockQuantity: 100 },
        { id: 3, name: 'Keyboard', stockQuantity: 100 },
    ];

    for (const product of products) {
        try {
            const response = await fetch(`${INVENTORY_API_URL}/api/inventory/${product.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    id: product.id,
                    stockQuantity: product.stockQuantity
                })
            });
            
            if (response.ok) {
                console.log(`  ✅ ${product.name}: Stock replenished to ${product.stockQuantity}`);
            } else {
                console.log(`  ⚠️ ${product.name}: Failed to replenish (${response.status})`);
            }
        } catch (error) {
            console.log(`  ⚠️ ${product.name}: Error - ${error.message}`);
        }
    }
    
    console.log('✅ Global Setup Complete\n');
}

module.exports = globalSetup;

