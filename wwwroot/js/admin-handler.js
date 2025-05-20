// Admin functionality handler

// to handle admin navigation with token
function setupAdminLinks() {
    // Get the JWT token from local storage
    const token = localStorage.getItem('token');
    if (!token) return;

    // Check if user is admin from token
    let isAdmin = false;
    try {
        const tokenPayload = JSON.parse(atob(token.split('.')[1]));
        isAdmin = tokenPayload.role === "Admin" || 
                tokenPayload["UserType"] === "Admin" ||
                (tokenPayload.role && Array.isArray(tokenPayload.role) && tokenPayload.role.includes("Admin"));
                
        // If user is not admin, don't proceed with admin link setup
        if (!isAdmin) {
            console.log("User is not admin, skipping admin link setup");
            return;
        }
        
        console.log("Admin user confirmed from token, setting up admin links");
    } catch (err) {
        console.error("Error checking admin status:", err);
        return;
    }

    // Update all admin links to include the token
    document.querySelectorAll('a[href^="/Admin/"]').forEach(link => {
        // Replace direct href with click handler for better token handling
        const originalHref = link.href;
        link.setAttribute('data-original-href', originalHref);
        
        // Add click handler instead of modifying href
        link.href = "javascript:void(0)";
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const href = this.getAttribute('data-original-href');
            navigateWithToken(href.split('?')[0]); // Remove any existing query parameters
        });
    });

    // For forms that post to admin controllers
    document.querySelectorAll('form.admin-form, form[action^="/Admin/"]').forEach(form => {
        // Create a hidden input for the token if it doesn't exist
        if (!form.querySelector('input[name="token"]')) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = 'token';
            tokenInput.value = token;
            form.appendChild(tokenInput);
        }
    });
    
    // Setup table sortable functionality
    setupTableSorting();
    
    // Setup search highlight
    highlightSearchTerms();
}

// to navigate to an admin page with the token
function navigateWithToken(url, params = {}) {
    const token = localStorage.getItem('token');
    if (!token) {
        console.error("No token available for admin navigation");
        return;
    }
    
    // Verify the token is valid before navigation
    try {
        const tokenPayload = JSON.parse(atob(token.split('.')[1]));
        const expirationTime = tokenPayload.exp * 1000; // Convert to milliseconds
        if (Date.now() > expirationTime) {
            console.error("Token expired, redirecting to login");
            localStorage.removeItem('token');
            window.location.href = "/AuthView/Login";
            return;
        }
    } catch (err) {
        console.error("Invalid token format, redirecting to login");
        localStorage.removeItem('token');
        window.location.href = "/AuthView/Login";
        return;
    }
    
    // Build the URL with parameters
    let finalUrl = url;
    const queryParams = new URLSearchParams();
    
    // Add token
    queryParams.append('token', token);
    
    // Add other params
    for (const [key, value] of Object.entries(params)) {
        queryParams.append(key, value);
    }
    
    // Append query string to URL
    finalUrl += '?' + queryParams.toString();
    
    // Set up AJAX headers for the next page
    if (typeof $ !== 'undefined' && $.ajax) {
        console.log("Setting Authorization header for next page");
        // Store token in session storage to ensure it's available after navigation
        sessionStorage.setItem('lastToken', token);
    }
    
    // Navigate
    console.log(`Navigating to: ${finalUrl}`);
    window.location.href = finalUrl;
}

// to make admin tables sortable
function setupTableSorting() {
    document.querySelectorAll('.table thead th').forEach(headerCell => {
        headerCell.addEventListener('click', () => {
            const tableElement = headerCell.closest('table');
            const headerIndex = Array.prototype.indexOf.call(headerCell.parentElement.children, headerCell);
            const currentIsAscending = headerCell.classList.contains('th-sort-asc');

            // Remove sort classes from all headers
            tableElement.querySelectorAll('th').forEach(th => {
                th.classList.remove('th-sort-asc', 'th-sort-desc');
            });

            // Add appropriate sort class to clicked header
            headerCell.classList.toggle('th-sort-asc', !currentIsAscending);
            headerCell.classList.toggle('th-sort-desc', currentIsAscending);

            // Get table rows and sort
            const tableBody = tableElement.querySelector('tbody');
            const rows = Array.from(tableBody.querySelectorAll('tr'));

            // Sort rows
            const sortedRows = rows.sort((a, b) => {
                const aColText = a.querySelector(`td:nth-child(${headerIndex + 1})`).textContent.trim();
                const bColText = b.querySelector(`td:nth-child(${headerIndex + 1})`).textContent.trim();

                return currentIsAscending
                    ? aColText.localeCompare(bColText, undefined, { numeric: true, sensitivity: 'base' })
                    : bColText.localeCompare(aColText, undefined, { numeric: true, sensitivity: 'base' });
            });

            // Remove all existing rows
            while (tableBody.firstChild) {
                tableBody.removeChild(tableBody.firstChild);
            }

            // Add sorted rows
            tableBody.append(...sortedRows);
        });
    });
}

// to highlight search terms in the table
function highlightSearchTerms() {
    // Get search term from URL
    const urlParams = new URLSearchParams(window.location.search);
    const searchTerm = urlParams.get('searchTerm');
    
    if (searchTerm && searchTerm.length > 0) {
        // Get all table cells
        document.querySelectorAll('table tbody td').forEach(cell => {
            const text = cell.textContent;
            if (text.toLowerCase().includes(searchTerm.toLowerCase())) {
                const regex = new RegExp('(' + searchTerm + ')', 'gi');
                cell.innerHTML = text.replace(regex, '<mark>$1</mark>');
            }
        });
    }
}

// to ensure Edit and Create links always have the token
function addTokenToEditLinks() {
    // Handle Edit links
    document.querySelectorAll('a[href^="/FlashcardsView/Edit/"]').forEach(link => {
        // Skip if already handled
        if (link.hasAttribute('data-token-handler')) {
            return;
        }
        
        // Mark as handled
        link.setAttribute('data-token-handler', 'true');
        
        // Replace with click handler
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const href = link.getAttribute('href');
            console.log(`Edit link clicked, navigating to ${href} with token`);
            navigateWithToken(href);
        });
    });
    
    // Handle Create links
    document.querySelectorAll('a[href^="/FlashcardsView/Create"]').forEach(link => {
        // Skip if already handled
        if (link.hasAttribute('data-token-handler')) {
            return;
        }
        
        // Mark as handled
        link.setAttribute('data-token-handler', 'true');
        
        // Replace with click handler
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const href = link.getAttribute('href');
            console.log(`Create link clicked, navigating to ${href} with token`);
            navigateWithToken(href);
        });
    });
    
    // Also handle Set view links for admins accessing private sets
    document.querySelectorAll('a[href^="/FlashcardsView/Set/"]').forEach(link => {
        // Skip if already handled by other scripts
        if (link.hasAttribute('data-token-handler')) {
            return;
        }
        
    // Setup AJAX Authorization header for admins
    setupAjaxAuthForAdmins();
        
        // Check if user is admin
        try {
            const token = localStorage.getItem('token');
            if (!token) return;
            
            const tokenPayload = JSON.parse(atob(token.split('.')[1]));
            const isAdmin = tokenPayload.role === "Admin" || 
                    tokenPayload["UserType"] === "Admin" ||
                    (tokenPayload.role && Array.isArray(tokenPayload.role) && tokenPayload.role.includes("Admin"));
                    
            if (isAdmin) {
                // Mark as handled
                link.setAttribute('data-token-handler', 'true');
                
                // Store original href
                const originalHref = link.getAttribute('href');
                
                // Replace with click handler for admins
                link.addEventListener('click', function(e) {
                    e.preventDefault();
                    console.log(`Admin accessing set link: ${originalHref}`);
                    navigateWithToken(originalHref);
                });
            }
        } catch (err) {
            console.error("Error processing admin access for set link:", err);
        }
    });
}

// Run when the DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    setupAdminLinks();
    addTokenToEditLinks();
    setupAjaxAuthForAdmins(); // to ensure all pages have proper token handling
});

// Also run after each AJAX request that could modify the page content
$(document).ajaxComplete(function() {
    setupAdminLinks();
    addTokenToEditLinks();
});

// Setup admin dashboard link with token in _Layout
document.addEventListener('DOMContentLoaded', function() {
    const adminDashboardLink = document.getElementById('adminDashboardLink');
    if (adminDashboardLink) {
        // Remove any existing event listeners (to avoid duplicates)
        const newAdminLink = adminDashboardLink.cloneNode(true);
        if (adminDashboardLink.parentNode) {
            adminDashboardLink.parentNode.replaceChild(newAdminLink, adminDashboardLink);
        }
        
        // Add event listener to the new element
        newAdminLink.addEventListener('click', function(e) {
            e.preventDefault();
            console.log("Admin panel link clicked, navigating with token");
            navigateWithToken('/Admin/ManageUsers');
        });
    }
});

// Add custom CSS for sorting indicators and other admin panel styling
document.addEventListener('DOMContentLoaded', function() {
    const style = document.createElement('style');
    style.textContent = `
        .th-sort-asc::after {
            content: "\\2191";
            margin-left: 5px;
        }
        
        .th-sort-desc::after {
            content: "\\2193";
            margin-left: 5px;
        }
        
        .table thead th {
            cursor: pointer;
            transition: background-color 0.2s;
        }
        
        .table thead th:hover {
            background-color: rgba(0,0,0,0.05);
        }
        
        mark {
            background-color: #fff3cd;
            padding: 0.1em 0.2em;
            border-radius: 3px;
        }
    `;
    document.head.appendChild(style);
});

// Setup AJAX Authentication header for admin users
function setupAjaxAuthForAdmins() {
    const token = localStorage.getItem('token');
    if (!token) return;
    
    console.log("Setting up global AJAX auth headers for authenticated user");
    
    // Set up global AJAX authorization header for all authenticated users
    if (typeof $ !== 'undefined' && $.ajaxSetup) {
        $.ajaxSetup({
            beforeSend: function(xhr) {
                // Add Authorization header to AJAX requests
                xhr.setRequestHeader("Authorization", "Bearer " + token);
            }
        });
    }
    
    // Also attach token to any forms that may be used for editing or creating
    document.querySelectorAll('form').forEach(form => {
        // Skip forms already processed
        if (form.hasAttribute('data-token-added')) {
            return;
        }
        
        form.setAttribute('data-token-added', 'true');
        
        if (!form.querySelector('input[name="jwt"]')) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = 'jwt';
            tokenInput.value = token;
            form.appendChild(tokenInput);
            console.log("Added JWT token to form:", form);
        }
        
        if (!form.querySelector('input[name="token"]')) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = 'token';
            tokenInput.value = token;
            form.appendChild(tokenInput);
            console.log("Added token input to form:", form);
        }
    });
    
    try {
        // Check if user is admin for admin-specific features
        const tokenPayload = JSON.parse(atob(token.split('.')[1]));
        const isAdmin = tokenPayload.role === "Admin" || 
                tokenPayload["UserType"] === "Admin" ||
                (tokenPayload.role && Array.isArray(tokenPayload.role) && tokenPayload.role.includes("Admin"));
        
        if (isAdmin) {
            console.log("Admin user detected, enabling admin-specific features");
            // Admin-specific features could be added here
        }
    } catch (err) {
        console.error("Error checking admin status:", err);
    }
}

// Document-level event delegation for edit and create links to handle dynamically added elements
document.addEventListener('click', function(e) {
    // Check if the click was on an edit link or inside one
    const editLink = e.target.closest('a[href^="/FlashcardsView/Edit/"]');
    
    if (editLink && !editLink.hasAttribute('data-token-handler')) {
        // If this link hasn't been handled by our specific handlers
        e.preventDefault();
        const href = editLink.getAttribute('href');
        console.log(`Caught click on unhandled edit link: ${href}`);
        navigateWithToken(href);
    }
    
    // Check if the click was on a create link or inside one
    const createLink = e.target.closest('a[href^="/FlashcardsView/Create"]');
    
    if (createLink && !createLink.hasAttribute('data-token-handler')) {
        // If this link hasn't been handled by our specific handlers
        e.preventDefault();
        const href = createLink.getAttribute('href');
        console.log(`Caught click on unhandled create link: ${href}`);
        navigateWithToken(href);
    }
});
