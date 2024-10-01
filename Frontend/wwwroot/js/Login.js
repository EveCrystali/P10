// /wwwroot/js/login.js
document.addEventListener("DOMContentLoaded", function() {
    const loginForm = document.getElementById('login-form'); // Si tu utilises un formulaire

    if (loginForm) {
        loginForm.addEventListener('submit', function(event) {
            event.preventDefault();
            const username = document.getElementById('username').value;
            const password = document.getElementById('password').value;

            fetch("http://localhost:5000/auth/login", {
                method: "POST",
                credentials: "include", // Inclure les cookies dans la requête
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ username, password })
            })
            .then(response => response.json())
            .then(data => {
                console.log("Logged in:", data);
                // Gérer la redirection ou les actions après connexion
            })
            .catch(error => {
                console.error("Error during login:", error);
            });
        });
    }
});