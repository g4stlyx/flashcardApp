// handles all links within the FlashcardsView that might need authentication tokens

document.addEventListener('DOMContentLoaded', function() {
    setupAuthenticatedLinksHandlers();
});

function setupAuthenticatedLinksHandlers() {
    // List of path patterns that should have token handling
    const authenticatedPathPatterns = [
        '/FlashcardsView/Set/',
        '/FlashcardsView/Study/',
        '/FlashcardsView/Edit/',
        '/FlashcardsView/UserSets/',
        '/FlashcardsView/Friends',
        '/FlashcardsView/MySets',
        '/FlashcardsView/Create'
    ];
    
    console.log('Setting up authenticated links handlers...');
    
    // Find all links that match our authenticated paths and don't already have handlers
    document.querySelectorAll('a').forEach(link => {
        // Skip if no href or already fully processed
        if (!link.href || link.getAttribute('data-token-handler') === 'processed') {
            return;
        }
        
        const href = link.href;
        console.log(`Checking link: ${href}`);
        
        // If the link already has a token handler attribute, set up the event listener
        if (link.hasAttribute('data-token-handler')) {
            console.log(`Link has data-token-handler attribute: ${href}`);
            setupLinkWithToken(link, href);
            return;
        }
        
        // Otherwise check if it matches our patterns
        const isAuthenticatedPath = authenticatedPathPatterns.some(pattern => 
            href.includes(pattern));
            
        if (!isAuthenticatedPath) {
            return;
        }
        
        console.log(`Link matches authenticated path pattern: ${href}`);
        setupLinkWithToken(link, href);
    });
}

// Helper function to set up token handling for a link
function setupLinkWithToken(link, href) {
    // Mark as fully processed
    link.setAttribute('data-token-handler', 'processed');
    
    // Save the original href
    const originalHref = href || link.href;
    
    console.log(`Setting up token handler for link: ${originalHref}`);
    
    link.addEventListener('click', function(e) {
        e.preventDefault();
        console.log(`Link clicked: ${originalHref}`);
        
        const token = localStorage.getItem('token');
        if (token) {
            console.log(`Token found in localStorage (length: ${token.length})`);
            console.log(`Adding token to request for: ${originalHref}`);
            
            // If we have a token, use navigateWithToken if available
            if (typeof navigateWithToken === 'function') {
                console.log(`Using navigateWithToken function for: ${originalHref}`);
                navigateWithToken(originalHref);
            } else {
                // Fallback: append token as query parameter
                console.log(`Using token query param fallback for: ${originalHref}`);
                const finalUrl = `${originalHref}?token=${encodeURIComponent(token)}`;
                console.log(`Final URL with token: ${finalUrl.substring(0, Math.min(100, finalUrl.length))}...`);
                window.location.href = finalUrl;
            }
        } else {
            // If no token, just go to the regular URL
            console.log(`No token found in localStorage, navigating without token to: ${originalHref}`);
            window.location.href = originalHref;
        }
    });
}

// Make the functions globally available
window.setupAuthenticatedLinksHandlers = setupAuthenticatedLinksHandlers;
window.setupLinkWithToken = setupLinkWithToken;

// Set up mutation observer to detect dynamically added links
const authLinksObserver = new MutationObserver(function(mutations) {
    let shouldSetup = false;
    
    mutations.forEach(function(mutation) {
        // Check if any new nodes were added
        if (mutation.addedNodes.length) {
            shouldSetup = true;
        }
    });
    
    // If relevant nodes were added, set up handlers
    if (shouldSetup) {
        setupAuthenticatedLinksHandlers();
    }
});

// Start observing the document body for changes
authLinksObserver.observe(document.body, {
    childList: true,
    subtree: true
});
