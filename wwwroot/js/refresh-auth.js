// Authentication state refresh handler

// Function to force a complete authentication refresh
function forceAuthRefresh() {
    console.log("Forcing complete authentication refresh...");
    
    // Store the current page URL to return to after refresh
    const currentUrl = window.location.href;
    sessionStorage.setItem('returnUrl', currentUrl);
    
    // Clear any cached user info
    localStorage.removeItem('lastUserInfo');
    sessionStorage.removeItem('userSessionData');
    
    // Reload from server (bypass cache)
    window.location.reload(true);
}

// Check for stale authentication on page visibility change
document.addEventListener('visibilitychange', function() {
    if (!document.hidden) {
        // When page becomes visible again, verify if token is still valid
        const token = localStorage.getItem('token');
        if (token) {
            const userInfo = window.getUserInfoFromToken();
            if (!userInfo) {
                // Invalid/expired token when page becomes visible again
                console.log("Authentication invalid on tab focus, refreshing state...");
                forceAuthRefresh();
            }
        }
    }
});

// Expose debug functions to global scope
window.debugAuth = {
    checkAuth: function() {
        const token = localStorage.getItem('token');
        const userInfo = window.getUserInfoFromToken();
        
        console.log("Current authentication state:");
        console.log("Token exists:", !!token);
        console.log("Valid user info:", !!userInfo);
        
        if (userInfo) {
            console.log("Logged in as:", userInfo.username);
            console.log("User ID:", userInfo.id);
            console.log("Is Admin:", userInfo.isAdmin);
        }
        
        return {
            isAuthenticated: !!userInfo,
            userInfo: userInfo
        };
    },
    
    clearAuth: function() {
        localStorage.removeItem('token');
        localStorage.removeItem('jwtBackup');
        localStorage.removeItem('userId');
        sessionStorage.clear();
        
        console.log("Authentication data cleared");
        return true;
    },
    
    refreshPage: function() {
        forceAuthRefresh();
    }
};
