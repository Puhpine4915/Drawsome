describe('Login Functionality', () => {
    it('should successfully log in with valid credentials', () => {
        cy.visit('http://localhost:5056/');
        
        cy.get('input[name="username"]').type('puhpine2');
        cy.get('input[name="password"]').type('test');
        
        cy.get('button').contains('Login').click();
        
        cy.contains('Logged in as puhpine2').should('be.visible');
    });

    it('should display an error message with invalid credentials (example)', () => {
        cy.visit('http://localhost:5056/');
        
        cy.get('input[name="username"]').type('puhpine2');
        cy.get('input[name="password"]').type('wrongpass');
        
        cy.get('button').contains('Login').click();
        
        cy.contains('Invalid username or password').should('be.visible');
    });
});