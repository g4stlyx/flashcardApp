// Private Sets Handler
// This script ensures that private sets can be accessed by owners and admins

document.addEventListener('DOMContentLoaded', function() {
    setupPrivateSetHandlers();
});

// This function should be called after any AJAX operation that might add new set links
function setupPrivateSetHandlers() {
    // Find all links to set pages and ensure they include tokens for private sets
    document.querySelectorAll('a[href^="/FlashcardsView/Set/"]').forEach(link => {
        // Skip links already handled by other scripts
        if (link.hasAttribute('data-token-handler') || link.classList.contains('view-set-btn')) {
            return;
        }

        // Mark as handled to avoid duplicate handlers
        link.setAttribute('data-token-handler', 'true');
        
        // Save the original href
        const originalHref = link.href;
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const token = localStorage.getItem('token');
            if (token) {
                // If we have a token, use navigateWithToken if available
                if (typeof navigateWithToken === 'function') {
                    navigateWithToken(originalHref);
                } else {
                    // Fallback: append token as query parameter
                    window.location.href = `${originalHref}?token=${encodeURIComponent(token)}`;
                }
            } else {
                // If no token, just go to the regular URL
                window.location.href = originalHref;
            }
        });
    });

    // Do the same for study links
    document.querySelectorAll('a[href^="/FlashcardsView/Study/"]').forEach(link => {
        // Skip links already handled by other scripts
        if (link.hasAttribute('data-token-handler') || link.classList.contains('study-set-btn')) {
            return;
        }

        // Mark as handled to avoid duplicate handlers
        link.setAttribute('data-token-handler', 'true');
        
        // Save the original href
        const originalHref = link.href;
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const token = localStorage.getItem('token');
            if (token) {
                // If we have a token, use navigateWithToken if available
                if (typeof navigateWithToken === 'function') {
                    navigateWithToken(originalHref);
                } else {
                    // Fallback: append token as query parameter
                    window.location.href = `${originalHref}?token=${encodeURIComponent(token)}`;
                }
            } else {
                // If no token, just go to the regular URL
                window.location.href = originalHref;
            }
        });
    });    // Handle Create links too
    document.querySelectorAll('a[href^="/FlashcardsView/Create"]').forEach(link => {
        // Skip links already handled by other scripts
        if (link.hasAttribute('data-token-handler') || link.classList.contains('create-set-btn')) {
            return;
        }

        // Mark as handled to avoid duplicate handlers
        link.setAttribute('data-token-handler', 'true');
        
        // Save the original href
        const originalHref = link.href;
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const token = localStorage.getItem('token');
            if (token) {
                // If we have a token, use navigateWithToken if available
                if (typeof navigateWithToken === 'function') {
                    navigateWithToken(originalHref);
                } else {
                    // Fallback: append token as query parameter
                    window.location.href = `${originalHref}?token=${encodeURIComponent(token)}`;
                }
            } else {
                // If no token, just go to the regular URL
                window.location.href = originalHref;
            }
        });
    });
    
    // Handle UserSets links for viewing friend sets
    document.querySelectorAll('a[href^="/FlashcardsView/UserSets/"]').forEach(link => {
        // Skip links already handled by other scripts
        if (link.hasAttribute('data-token-handler')) {
            return;
        }

        // Mark as handled to avoid duplicate handlers
        link.setAttribute('data-token-handler', 'true');
        
        // Save the original href
        const originalHref = link.href;
        
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const token = localStorage.getItem('token');
            if (token) {
                // If we have a token, use navigateWithToken if available
                if (typeof navigateWithToken === 'function') {
                    navigateWithToken(originalHref);
                } else {
                    // Fallback: append token as query parameter
                    window.location.href = `${originalHref}?token=${encodeURIComponent(token)}`;
                }
            } else {
                // If no token, just go to the regular URL
                window.location.href = originalHref;
            }
        });
    });
}

// Make the function globally available
window.setupPrivateSetHandlers = setupPrivateSetHandlers;

// Set up mutation observer to detect dynamically added set links
const observer = new MutationObserver(function(mutations) {
    let shouldSetup = false;
    
    mutations.forEach(function(mutation) {
        // Check if any new nodes were added
        if (mutation.addedNodes.length) {
            mutation.addedNodes.forEach(function(node) {
                // Check if the node is an element
                if (node.nodeType === Node.ELEMENT_NODE) {
                // Check if it contains any set links
                    if (node.querySelector && (
                        node.querySelector('a[href^="/FlashcardsView/Set/"]') ||
                        node.querySelector('a[href^="/FlashcardsView/Study/"]') ||
                        node.querySelector('a[href^="/FlashcardsView/UserSets/"]')
                    )) {
                        shouldSetup = true;
                    }
                }
            });
        }
    });
    
    // If relevant nodes were added, set up handlers
    if (shouldSetup) {
        setupPrivateSetHandlers();
    }
});

// Start observing the document body for changes
observer.observe(document.body, {
    childList: true,
    subtree: true
});
