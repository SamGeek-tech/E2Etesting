# Testing Strategy & E2E Scenarios

## 1. Test Pyramid Definition

Our testing strategy follows the industry-standard pyramid:

- **Unit Tests (Base):** Validate individual business logic components (e.g., Order calculation, stock validation logic). Mandatory gate for PRs.
- **API Integration Tests (Middle):** Validate service-to-service communication and database persistence. Mandatory gate for Build Pipeline.
- **E2E UI Tests (Top):** Validate critical user journeys via the browser. Mandatory gate for Deployment to Staging/Production.

**Mandatory Gates:**
1. **CI Build:** Must pass all Unit & Integration tests.
2. **CD Release:** Must pass automated E2E Smoke Tests before promotion.

---

## 2. E2E UI Scenarios (Playwright)

| ID | Scenario | Description | Backend Services Involved |
|----|----------|-------------|----------------------------|
| 1 | **Successful Authentication** | User logs in with valid credentials and sees the dashboard. | OrderService (Identity) |
| 2 | **Place Order Flow** | User selects a product, specifies quantity, and completes order. | InventoryService, OrderService |
| 3 | **Order History Validation** | User places an order, then verifies it appears in "My Orders". | OrderService |
| 4 | **Stock Depletion** | User orders the last item; verifies it becomes "Sold Out" or disabled. | InventoryService, OrderService |
| 5 | **Failed Authentication** | User attempts login with wrong password and sees error. | OrderService (Identity) |
| 6 | **Empty Orders State** | Verifies the "No orders" message for users with zero history. | OrderService |
| 7 | **Bulk Quantity Order** | Verifies ordering multiple units of an item correctly updates totals. | InventoryService, OrderService |
| 8 | **Out of Stock UI** | Verifies the "Order" button is disabled when stock reaches zero. | InventoryService |
| 9 | **Layout Navigation** | Verifies all header links (Home, Shop, Orders) navigate correctly. | Web Portal |
| 10 | **Session Termination** | Verifies Logout redirects to login and clears access to protected pages. | OrderService (Identity) |
