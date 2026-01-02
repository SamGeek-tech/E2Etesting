class LoginPage {
    visit() {
        cy.visit('/Account/Login');
    }

    login(email, password) {
        cy.get('#email').type(email);
        cy.get('#password').type(password);
        cy.get('#login-button').click();
    }

    getErrorMessage() {
        return cy.get('.text-danger');
    }
}

export default new LoginPage();
