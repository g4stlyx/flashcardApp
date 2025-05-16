// Special handler for Edit pages
document.addEventListener("DOMContentLoaded", function() {
    console.log("Edit page handler loaded");
    
    // Check if we're on an Edit page
    if (window.location.pathname.includes("/FlashcardsView/Edit/")) {
        const token = localStorage.getItem("token");
        
        if (!token) {
            console.log("No token found for Edit page, redirecting to login");
            window.location.href = "/AuthView/Login";
            return;
        }
        
        console.log("Found token for Edit page, length:", token.length);
        
        // Add a hidden form field with the token to any forms on the page
        const forms = document.querySelectorAll("form");
        forms.forEach(form => {
            // Check if the form already has a token field
            if (!form.querySelector('input[name="jwt"]')) {
                const tokenInput = document.createElement("input");
                tokenInput.type = "hidden";
                tokenInput.name = "jwt";
                tokenInput.value = token;
                form.appendChild(tokenInput);
                console.log("Added token to form:", form.id || "[unnamed form]");
            }
        });
        
        // Add an event listener to automatically add token to dynamically created forms
        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeName === "FORM") {
                        // Check if the form already has a token field
                        if (!node.querySelector('input[name="jwt"]')) {
                            const tokenInput = document.createElement("input");
                            tokenInput.type = "hidden";
                            tokenInput.name = "jwt";
                            tokenInput.value = token;
                            node.appendChild(tokenInput);
                            console.log("Added token to dynamically created form");
                        }
                    }
                });
            });
        });
        
        observer.observe(document.body, { childList: true, subtree: true });
        
        // Add Authorization header to all AJAX requests
        if (typeof $ !== 'undefined' && $.ajax) {
            $.ajaxSetup({
                beforeSend: function(xhr) {
                    xhr.setRequestHeader("Authorization", `Bearer ${token}`);
                    console.log("Added Authorization header to AJAX request");
                }
            });
        }
        
        // If the URL doesn't have a token parameter, add it
        if (!window.location.search.includes("token=")) {
            const currentUrl = new URL(window.location.href);
            currentUrl.searchParams.set("token", token);
            
            // Use history.replaceState to update the URL without refreshing
            window.history.replaceState({}, document.title, currentUrl.toString());
            console.log("Added token to URL without refreshing");
        }
    }
});
