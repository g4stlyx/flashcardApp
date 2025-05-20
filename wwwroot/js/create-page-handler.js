// ensures that forms on the Create page include the authentication token

document.addEventListener('DOMContentLoaded', function() {
    // Get form elements
    const createForm = document.querySelector('#createSetForm');
    
    // If form exists on this page
    if (createForm) {
        console.log("Found create form, setting up token handling");
        
        // Make sure token is included
        const token = localStorage.getItem('token');
        if (token) {
            // Add token to form if not already present
            if (!createForm.querySelector('input[name="token"]')) {
                const tokenInput = document.createElement('input');
                tokenInput.type = 'hidden';
                tokenInput.name = 'token';
                tokenInput.value = token;
                createForm.appendChild(tokenInput);
                console.log("Added token to create form");
            }
            
            // Add JWT to form if not already present
            if (!createForm.querySelector('input[name="jwt"]')) {
                const jwtInput = document.createElement('input');
                jwtInput.type = 'hidden';
                jwtInput.name = 'jwt';
                jwtInput.value = token;
                createForm.appendChild(jwtInput);
                console.log("Added JWT to create form");
            }
            
            // Also setup global AJAX auth if jQuery is available
            if (typeof $ !== 'undefined' && $.ajaxSetup) {
                $.ajaxSetup({
                    beforeSend: function(xhr) {
                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                    }
                });
                console.log("Set up AJAX auth header for Create page");
            }
        } else {
            console.warn("No token available for Create page form");
        }
    }
    
    // Also handle any other forms on the page
    document.querySelectorAll('form:not(#createSetForm)').forEach(form => {
        if (form.hasAttribute('data-token-added')) {
            return;
        }
        
        console.log("Found additional form, adding token", form);
        
        form.setAttribute('data-token-added', 'true');
        
        const token = localStorage.getItem('token');
        if (token) {
            if (!form.querySelector('input[name="token"]')) {
                const tokenInput = document.createElement('input');
                tokenInput.type = 'hidden';
                tokenInput.name = 'token';
                tokenInput.value = token;
                form.appendChild(tokenInput);
            }
            
            if (!form.querySelector('input[name="jwt"]')) {
                const jwtInput = document.createElement('input');
                jwtInput.type = 'hidden';
                jwtInput.name = 'jwt';
                jwtInput.value = token;
                form.appendChild(jwtInput);
            }
        }
    });
});
