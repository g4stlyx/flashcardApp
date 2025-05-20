// Special handler for MySets page

document.addEventListener("DOMContentLoaded", function() {
    console.log("MySets page handler loaded");
    
    // Check if we're on MySets page
    if (window.location.pathname.includes("/FlashcardsView/MySets")) {
        console.log("MySets page detected, checking authentication");
        
        // Get the token
        const token = localStorage.getItem("token");
        if (!token) {
            console.log("No token found, redirecting to login");
            window.location.href = "/AuthView/Login";
            return;
        }
        
        // Force the login/logout UI update
        // This ensures the header shows logout instead of login
        $('#loginItem').addClass('d-none');
        $('#logoutItem').removeClass('d-none');
        
        // Get user info
        const userInfo = window.getUserInfoFromToken();
        if (userInfo) {
            $('#usernameDisplay').text(userInfo.username);
            $('#mySetsItem').show();
            
            // Add admin indicator if user is admin
            if (userInfo.isAdmin) {
                $('#usernameDisplay').after(' <span class="badge bg-danger">Admin</span>');
            }
            
            // Verify we're showing the right user's sets
            const userId = parseInt(userInfo.userId, 10);
            console.log("Current user ID from token:", userId);
            
            // Store the user ID in local storage for reference
            localStorage.setItem("userId", userId.toString());
            
            // Check if the page is showing the right sets for this user
            console.log("Verifying page shows correct user's sets...");
            
            // If we have URL parameters, check if there's a userId parameter
            const urlParams = new URLSearchParams(window.location.search);
            const urlUserId = urlParams.get('userId');
            
            // If we don't have a userId parameter in the URL, add it to force correct user sets
            if (!urlUserId) {
                console.log("Adding userId to URL to ensure correct sets are shown");
                urlParams.append('userId', userId.toString());
                
                // Construct new URL with userId parameter
                const newUrl = window.location.pathname + "?" + urlParams.toString();
                
                // Update URL without reloading page
                window.history.replaceState({}, '', newUrl);
            }
            // If we have a userId parameter but it doesn't match our token's userId, reload with correct ID
            else if (urlUserId !== userId.toString()) {
                console.log("URL userId doesn't match token userId, reloading with correct ID");
                urlParams.set('userId', userId.toString());
                
                // Construct new URL with updated userId parameter
                const newUrl = window.location.pathname + "?" + urlParams.toString();
                
                // Reload page with correct userId
                window.location.href = newUrl;
            }
        } else {
            console.log("Invalid or expired token, redirecting to login");
            localStorage.removeItem("token");
            window.location.href = "/AuthView/Login";
        }
    }
});
