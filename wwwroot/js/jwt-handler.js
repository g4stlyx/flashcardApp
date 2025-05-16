// JWT Handler for MVC Views

// IMMEDIATE jQuery AJAX setup - do this at the top level before any events
// This ensures any script using jQuery has the token set up
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
      currentPath.includes("/FlashcardsView/Study/");
      
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

      // Add the token as a hidden field
      const tokenInput = document.createElement("input");
      tokenInput.type = "hidden";
      tokenInput.name = "jwt";
      tokenInput.value = token;
      form.appendChild(tokenInput);

      // Add CSRF token if it exists
      const csrfToken = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      );
      if (csrfToken) {
        const csrfInput = document.createElement("input");
        csrfInput.type = "hidden";
        csrfInput.name = "__RequestVerificationToken";
        csrfInput.value = csrfToken.value;
        form.appendChild(csrfInput);
      }

      // Submit the form
      document.body.appendChild(form);
      console.log("Form created, submitting it now...");
      form.submit();
    } else {      // For MySets and other GET pages, always use query parameter 
      console.log("Direct navigation to:", href);
      
      // We're always using query parameter now since cookies are not working
      console.log("Using query parameter for token");
      
      // Append token as query parameter
      const separator = href.includes('?') ? '&' : '?';
      window.location.href = href + separator + "token=" + encodeURIComponent(token);
      }
    }, 100);
  }; // Small delay of 100ms

document.addEventListener("DOMContentLoaded", function () {
  // Check if we're on a protected page and redirect if needed
  const isProtectedPage =
    window.location.pathname.includes("/FlashcardsView/MySets") ||
    window.location.pathname.includes("/FlashcardsView/Create") ||
    window.location.pathname.includes("/FlashcardsView/Edit/");

  if (isProtectedPage) {
    const token = localStorage.getItem("token");
    console.log("Checking token on protected page:", window.location.pathname);
    
    if (!token) {
      console.log(
        "Protected page accessed without token, redirecting to login"
      );
      window.location.href = "/AuthView/Login";
      return;    } else {
      // If this is a direct navigation and we have a token, add token as query param
      if (window.location.pathname === "/FlashcardsView/MySets") {
        console.log(
          "Detected direct navigation to MySets, adding token as query param"
        );
        if (!window.location.search.includes('token=')) {
          // Add token as query parameter if not already there
          const separator = window.location.search ? '&' : '?';
          window.location.href = window.location.pathname + separator + "token=" + encodeURIComponent(token);
        }
        return;
      }
      
      // Special handling for Edit pages that were directly navigated to
      if (window.location.pathname.includes("/FlashcardsView/Edit/")) {
        console.log("Detected direct navigation to Edit page");
        
        // If no token in query string, try to add it
        if (!window.location.search.includes('token=')) {
          console.log("No token in query string for Edit page, adding it");
          const separator = window.location.search ? '&' : '?';
          window.location.href = window.location.pathname + separator + "token=" + encodeURIComponent(token);
          return;
        }
      }
    }
  }

  // Debug function to check if the token is valid  window.validateJwtToken = function () {
    const token = localStorage.getItem("token");
    if (!token) {
      console.error("No token found in localStorage");
      return false;
    }

    try {
      const claims = parseJwt(token);
      if (!claims) {
        console.error("Token could not be parsed");
        return false;
      }

      // Check expiration
      if (claims.exp) {
        const expDate = new Date(claims.exp * 1000);
        const now = new Date();
        if (expDate <= now) {
          console.error("Token expired on", expDate.toLocaleString());
          return false;
        }
        console.log("Token valid until", expDate.toLocaleString());
      }
      
      // Check for nameid and verify it's a valid numeric ID
      if (!claims.nameid) {
        console.error("Token missing user ID (nameid claim)");
        return false;
      }
      
      const userId = parseInt(claims.nameid, 10);
      if (isNaN(userId) || userId <= 0) {
        console.error("Invalid user ID in token:", claims.nameid);
        return false;
      }
      
      console.log("Token validated, User ID:", userId);
      
      // Update user_id cookie to ensure consistent identification
      document.cookie = `user_id=${userId}; path=/; max-age=${60*60*24*7}`; // 7 days
      
      // Store the numeric user ID in localStorage too
      localStorage.setItem("userId", userId.toString());

      console.log("Token claims:", claims);
      return true;
    } catch (e) {
      console.error("Error validating token:", e);
      return false;
    }
  },

  // Function to get user info from JWT token
  window.getUserInfoFromToken = function () {
    const token = localStorage.getItem("token");
    if (!token) return null;

    const claims = parseJwt(token);
    if (!claims) return null;

    // Log all claims for debugging
    console.log("All JWT claims in token:", claims);

    return {
      username: claims.unique_name || claims.name,
      userId: claims.nameid,
      // Make sure nameid is returned as a number - parseInt for safety
      userIdNumber: parseInt(claims.nameid, 10),
      isAdmin: claims.role === "Admin" || claims["UserType"] === "Admin",
      email: claims.email,
    };
  });

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
  ];

  // Find and modify links to protected paths
  function setupProtectedLinks() {
    protectedPaths.forEach((item) => {
      const links = document.querySelectorAll(item.selector);
      links.forEach((link) => {
        if (link.getAttribute("data-jwt-handler-attached") === "true") {
          return; // Skip already processed links
        }
        // Update the link to use our custom handler
        link.addEventListener("click", function (e) {
          e.preventDefault();
          handleProtectedLink(link.getAttribute("href"));
        });
        // Add a data attribute to mark as processed
        link.setAttribute("data-jwt-handler-attached", "true");
      });
    });
  }

  // Set up the protected links initially and also when DOM changes
  setupProtectedLinks();
  
  // Generic handler for all links to protected paths
  document.addEventListener("click", function (e) {
    // Check if the click was on an <a> element
    let targetElement = e.target;
    while (targetElement && targetElement.tagName !== "A") {
      targetElement = targetElement.parentElement;
    }

    if (!targetElement) return; // Not a link click

    // Skip if we've already processed this link
    if (targetElement.getAttribute("data-jwt-handler-attached") === "true")
      return;

    const href = targetElement.getAttribute("href");
    if (!href) return; // No href attribute

    // Check if this is a protected path
    const isProtected = protectedPaths.some((item) =>
      href.startsWith(item.path)
    );
    if (!isProtected) return; // Not a protected path

    // We have a protected link, handle it
    e.preventDefault();
    handleProtectedLink(href);
  });
  
  // Re-scan the DOM periodically for new protected links
  setInterval(setupProtectedLinks, 1000);
  // Add Authorization header to AJAX requests
  if (localStorage.getItem("token")) {
    // Override fetch to include Authorization header
    const originalFetch = window.fetch;
    window.fetch = function (url, options = {}) {
      options = options || {};
      options.headers = options.headers || {};
      options.headers["Authorization"] = `Bearer ${localStorage.getItem(
        "token"
      )}`;
      return originalFetch(url, options);
    };
    
    // Also override jQuery AJAX if jQuery is available
    if (typeof $ !== 'undefined' && $.ajax) {
      console.log("Adding JWT token to all jQuery AJAX requests");
      $.ajaxSetup({
        beforeSend: function(xhr) {
          const token = localStorage.getItem("token");
          if (token) {
            xhr.setRequestHeader("Authorization", `Bearer ${token}`);
          }
        }
      });
    }
      // We're not setting cookies anymore since they don't work reliably
    // Instead, we're using the Authorization header for all requests
    
    // Add a periodic check to ensure token is still valid
    setInterval(() => {
      const token = localStorage.getItem("token");
      if (token) {
        try {
          const claims = parseJwt(token);
          if (isTokenExpired(claims)) {
            console.log("Token expired during session, you'll need to log in again");
            localStorage.removeItem("token");
            if (!window.location.pathname.includes("/AuthView/Login")) {
              window.location.href = "/AuthView/Login";
            }
          }
        } catch (e) {
          console.error("Error validating token during periodic check:", e);
        }
      }
    }, 60000); // Check every minute
    
    // Log success of our authentication strategy
    console.log("Using Authorization header-based authentication strategy");
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
      currentPath.includes("/FlashcardsView/Study/")
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
      currentPath.includes("/FlashcardsView/Study/")
    )) {
      console.log("Back button to protected page but no token, redirecting to login");
      window.location.href = "/AuthView/Login";
    }
  }
