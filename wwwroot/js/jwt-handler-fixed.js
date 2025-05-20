// JWT Handler for MVC Views

(function() {
  const token = localStorage.getItem("token");
  if (token && typeof $ !== 'undefined' && $.ajax) {
    console.log("Setting up immediate Authorization header for jQuery AJAX");
    $.ajaxSetup({
      beforeSend: function(xhr) {
        xhr.setRequestHeader("Authorization", `Bearer ${token}`);
      }
    });
    
    // Also patch the $.ajax method directly
    const originalAjax = $.ajax;
    $.ajax = function(settings) {
      settings = settings || {};
      settings.headers = settings.headers || {};
      if (!settings.headers["Authorization"]) {
        settings.headers["Authorization"] = `Bearer ${token}`;
      }
      return originalAjax.call($, settings);
    };
  }
  // Check token on page load to handle browser history navigation
  document.addEventListener('DOMContentLoaded', checkTokenOnNavigate);
  
  // Also handle when the user navigates with browser history (back/forward buttons)
  window.addEventListener('popstate', function(event) {
    console.log("Back/forward button navigation detected");
    checkTokenOnNavigate();
    handleBackButtonNavigation();
  });
  
  function checkTokenOnNavigate() {
    console.log("Navigation detected, checking token state...");
    // Get the current token
    const token = localStorage.getItem("token");
    const currentPath = window.location.pathname;
    
    // Track the navigation history
    const prevPage = sessionStorage.getItem("currentPage");
    sessionStorage.setItem("previousPage", prevPage || "");
    sessionStorage.setItem("currentPage", currentPath);
    
    console.log(`Navigation from ${prevPage || 'unknown'} to ${currentPath}`);
    
    // Check if we're on a protected page
    const isProtectedPage = 
      currentPath.includes("/FlashcardsView/MySets") ||
      currentPath.includes("/FlashcardsView/Create") ||
      currentPath.includes("/FlashcardsView/Edit/") ||
      currentPath.includes("/FlashcardsView/Study/") ||
      currentPath.includes("/FlashcardsView/Friends") ||
      currentPath.includes("/FlashcardsView/FriendSets") ||
      currentPath.includes("/FlashcardsView/UserSets/");
      
    console.log("Current path:", currentPath, "Protected:", isProtectedPage);

    if (isProtectedPage) {
      console.log("Protected page detected, token:", token ? "present" : "absent");
      
      // If we're on a protected page but don't have a token, check if we have a backup token
      if (!token) {
        const backupToken = localStorage.getItem("jwtBackup");
        if (backupToken) {
          console.log("No primary token found but backup exists, restoring from backup");
          localStorage.setItem("token", backupToken);
          token = backupToken;
        } else {
          console.log("No token found after navigation to protected page, redirecting to login");
          window.location.replace("/AuthView/Login");
          return;
        }
      }
      
      // Always ensure token is in URL for protected pages (this helps with back button)
      if (!window.location.search.includes("token=")) {
        console.log("Adding token to URL after navigation");
        const separator = window.location.search ? '&' : '?';
        const newUrl = window.location.pathname + window.location.search + 
                      separator + "token=" + encodeURIComponent(token);
        // Use replaceState to avoid creating new history entries
        window.history.replaceState({}, document.title, newUrl);
        
        // Also setup AJAX headers again to be extra safe
        if (typeof $ !== 'undefined' && $.ajax) {
          console.log("Re-establishing AJAX Authorization headers after navigation");
          $.ajaxSetup({
            beforeSend: function(xhr) {
              xhr.setRequestHeader("Authorization", `Bearer ${token}`);
            }
          });
        }
      }
    }
  }
})();

// Helper function to parse JWT tokens
function parseJwt(token) {
  if (!token) return null;
  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map(function (c) {
          return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2);
        })
        .join("")
    );

    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error("Error parsing JWT token:", e);
    return null;
  }
}

// Helper function to check if token is expired
function isTokenExpired(parsedTokenClaims) {
  if (!parsedTokenClaims || typeof parsedTokenClaims.exp === "undefined") {
    // No claims or no expiration claim, assume not expired or rely on server.
    // For stricter client-side validation, one might treat absence of 'exp' as an issue.
    console.log("Token has no expiration claim or claims object is invalid.");
    return false;
  }
  const expirationTimeInSeconds = parsedTokenClaims.exp;
  const nowInSeconds = Math.floor(Date.now() / 1000);

  if (expirationTimeInSeconds < nowInSeconds) {
    console.log(
      "Token expired at: " +
        new Date(expirationTimeInSeconds * 1000).toLocaleString()
    );
    return true; // Token is expired
  }
  console.log(
    "Token valid until: " +
      new Date(expirationTimeInSeconds * 1000).toLocaleString()
  );
  return false; // Token is not expired
}

// Define handleProtectedLink function in the global scope so it can be called from inline onclick handlers
function handleProtectedLink(href) {
  // Get the JWT token
  const token = localStorage.getItem("token");
  if (!token) {
    // No token, redirect to login
    console.log("No token found, redirecting to login");
    window.location.href = "/AuthView/Login";
    return;
  }

  let claims;
  try {
    claims = parseJwt(token); // parseJwt returns null on error
    if (!claims) {
      console.error(
        "Invalid token format: failed to parse or token is malformed."
      );
      localStorage.removeItem("token");
      window.location.href = "/AuthView/Login";
      return;
    }

    // Check for token expiration
    if (isTokenExpired(claims)) {
      console.log("Token is expired. Redirecting to login.");
      localStorage.removeItem("token"); // Remove the expired token
      window.location.href = "/AuthView/Login";
      return;
    }

    console.log("Token claims appear valid and not expired:", claims);
  } catch (e) {
    // This catch is a fallback, primarily if parseJwt itself had an unhandled exception,
    // though it's designed to return null on internal errors.
    console.error("Error processing token (unexpected exception):", e);
    localStorage.removeItem("token");
    window.location.href = "/AuthView/Login";
    return;
  }
  
  console.log("Handling protected link to:", href);  
  // We're no longer focusing on setting cookies since they don't work consistently
  // Instead, we'll rely on localStorage and sending the token via Authorization header
  
  // Store a backup of the token
  localStorage.setItem("jwtBackup", token);
  
  console.log("Using JWT token for authorization:", token.substring(0, 20) + "...");
  // Add a small delay to ensure cookie is set before navigation
  setTimeout(() => {
    console.log("After delay, proceeding with navigation...");

    // Verify token is still valid before proceeding
    try {
      const tokenClaims = parseJwt(token);
      if (isTokenExpired(tokenClaims)) {
        console.error("Token expired before navigation, redirecting to login");
        localStorage.removeItem("token");
        window.location.href = "/AuthView/Login";
        return;
      }
    } catch (e) {
      console.error("Error re-validating token:", e);
    }
    
    // Test cookie status
    const cookieExists = document.cookie.includes("jwt=");
    console.log("Cookie status before navigation - exists:", cookieExists);
    // Only use POST for Create/Edit/Study, otherwise do normal navigation
    if (
      href.startsWith("/FlashcardsView/Create") ||
      href.startsWith("/FlashcardsView/Edit/") ||
      href.startsWith("/FlashcardsView/Study/")
    ) {
      console.log("Using POST form submission with token for:", href);
      // For Edit pages, try both POST form and query parameter approach
      if (href.startsWith("/FlashcardsView/Edit/")) {
        // First try the query parameter approach as a fallback
        console.log("Adding query parameter backup for Edit page");
        const separator = href.includes('?') ? '&' : '?';
        const modifiedHref = href + separator + "token=" + encodeURIComponent(token);
        
        // Set Authorization header in localStorage for the next page load
        localStorage.setItem("lastAuthPath", href);
        localStorage.setItem("authInProgress", "true");
        
        // Change action from Edit to EditPost for POST form submission
        href = href.replace("/FlashcardsView/Edit/", "/FlashcardsView/EditPost/");
        console.log("Changed form action to:", href);
      }
      
      // Create a form to POST with the token
      const form = document.createElement("form");
      form.method = "POST";
      form.action = href;
      form.style.display = "none";

      // Create a hidden input for the token
      const input = document.createElement("input");
      input.type = "hidden";
      input.name = "token";
      input.value = token;

      // Add the input to the form and the form to the body
      form.appendChild(input);
      document.body.appendChild(form);

      // Submit the form
      form.submit();
    } else {
      // For other pages, just add the token as a query parameter
      const separator = href.includes('?') ? '&' : '?';
      const modifiedHref = href + separator + "token=" + encodeURIComponent(token);
      
      // Set Authorization header in localStorage for the next page load
      localStorage.setItem("lastAuthPath", href);
      localStorage.setItem("authInProgress", "true");
      
      window.location.href = modifiedHref;
    }
  }, 50); // Small delay, just for safety
}

// Set up event handlers for navigation links when the DOM is ready
document.addEventListener("DOMContentLoaded", function () {
  // Check for token on page load and update UI
  // Trigger this immediately to avoid flash of unauthenticated content/UI
  if (typeof $ !== "undefined") {
    $(document).ready(function () {
      console.log("DOM ready, updating UI based on authentication state");
      
      // Check browser-stored token
      const token = localStorage.getItem("token");
      if (token) {
        // Basic validation - assume we're logged in if token exists
        // Detailed validation will happen below in the parseJwt step
        console.log("Token found in storage on page load");
        
        // Setup AJAX Authorization Header at the same time
        if ($.ajax) {
          $.ajaxSetup({
            beforeSend: function(xhr) {
              xhr.setRequestHeader("Authorization", `Bearer ${token}`);
            }
          });
        }
        
      } else {
        console.log("No token found on page load");
      }
      
      // Start periodic token check every minute
      setInterval(function() {
        const token = localStorage.getItem('token');
        if (token) {
          // Parse and validate token
          const claims = parseJwt(token);
          if (claims && !isTokenExpired(claims)) {
            console.log("Token valid on periodic check");
          } else {
            console.log("Token invalid or expired on periodic check");
            localStorage.removeItem('token');
            
            // Only force a reload if we're on a protected page
            const currentPath = window.location.pathname;
            if (currentPath.includes("/FlashcardsView/") || 
                currentPath.includes("/Admin/")) {
                console.log("On protected page with invalid token, redirecting to login");
                window.location.href = "/AuthView/Login";
            }
          }
        }
      }, 60000); // Check every minute
      
      // Log success of our authentication strategy
      console.log("Using Authorization header-based authentication strategy");
    });
  }
  
  // Function to handle back button navigation specifically
  function handleBackButtonNavigation() {
    const token = localStorage.getItem("token");
    const currentPath = window.location.pathname;
    // If we have a token and we're on a protected page
    if (token && (
      currentPath.includes("/FlashcardsView/MySets") ||
      currentPath.includes("/FlashcardsView/Create") ||
      currentPath.includes("/FlashcardsView/Edit/") ||
      currentPath.includes("/FlashcardsView/Study/") ||
      currentPath.includes("/FlashcardsView/Friends") ||
      currentPath.includes("/FlashcardsView/FriendSets") ||
      currentPath.includes("/FlashcardsView/UserSets/")
    )) {
      console.log("Back button detected on protected page, ensuring token is applied");
      
      // For edit and study pages, reload with token parameter if missing
      if ((currentPath.includes("/FlashcardsView/Edit/") || 
           currentPath.includes("/FlashcardsView/Study/")) && 
          !window.location.search.includes("token=")) {
        
        console.log("Back navigation to protected page, adding token parameter");
        const separator = window.location.search ? '&' : '?';
        const newUrl = window.location.pathname + separator + "token=" + encodeURIComponent(token);
        
        // Use replace to avoid adding to history
        window.location.replace(newUrl);
        return;
      }
      
      // For all protected pages, ensure jQuery auth headers are set
      if (typeof $ !== 'undefined' && $.ajax) {
        console.log("Re-applying Authorization headers after back navigation");
        $.ajaxSetup({
          beforeSend: function(xhr) {
            xhr.setRequestHeader("Authorization", `Bearer ${token}`);
          }
        });
      }
    }
    // If we don't have a token but we're on a protected page, redirect to login
    if (!token && (
      currentPath.includes("/FlashcardsView/MySets") ||
      currentPath.includes("/FlashcardsView/Create") ||
      currentPath.includes("/FlashcardsView/Edit/") ||
      currentPath.includes("/FlashcardsView/Study/") ||
      currentPath.includes("/FlashcardsView/Friends") ||
      currentPath.includes("/FlashcardsView/FriendSets") ||
      currentPath.includes("/FlashcardsView/UserSets/")
    )) {
      console.log("Back button to protected page but no token, redirecting to login");
      window.location.href = "/AuthView/Login";
    }
  }
});

// Function to get user info from JWT token - globally accessible
window.getUserInfoFromToken = function() {
  const token = localStorage.getItem("token");
  if (!token) {
    console.log("No token found in localStorage");
    return null;
  }

  const claims = parseJwt(token);
  if (!claims) {
    console.log("Failed to parse token claims");
    return null;
  }
  
  // Check if token is expired
  if (isTokenExpired(claims)) {
    console.log("Token is expired, clearing from storage");
    localStorage.removeItem('token');
    return null;
  }

  // Log all claims for debugging
  console.log("All JWT claims in token:", claims);

  // Check for admin role in multiple possible claim locations
  const isAdmin = 
    claims.role === "Admin" || 
    claims["UserType"] === "Admin" ||
    (claims.role && Array.isArray(claims.role) && claims.role.includes("Admin")) ||
    (claims.roles && Array.isArray(claims.roles) && claims.roles.includes("Admin"));

  console.log("User has admin privileges:", isAdmin);

  return {
    id: claims.nameid,
    username: claims.unique_name || claims.name,
    userIdNumber: parseInt(claims.nameid, 10),
    isAdmin: isAdmin,
    email: claims.email,
  };
};

// Only handle specific protected paths
const protectedPaths = [
  { path: "/FlashcardsView/MySets", selector: "#mySetsItem a" },
  {
    path: "/FlashcardsView/Create",
    selector: 'a[href*="/FlashcardsView/Create"]',
  },
  {
    path: "/FlashcardsView/Edit/",
    selector: 'a[href*="/FlashcardsView/Edit/"]',
  },
  {
    path: "/FlashcardsView/Study/",
    selector: 'a[href*="/FlashcardsView/Study/"]',
  },
  {
    path: "/FlashcardsView/Friends",
    selector: "#friendsItem a",
  },
  {
    path: "/FlashcardsView/FriendSets",
    selector: "#friendSetsItem a",
  },
  {
    path: "/FlashcardsView/UserSets/",
    selector: 'a[href*="/FlashcardsView/UserSets/"]',
  },
];
