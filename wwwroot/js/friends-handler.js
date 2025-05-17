// Friends functionality handler for the Flashcard App
// This script handles showing the friends menu items and notifications for logged in users

document.addEventListener('DOMContentLoaded', function() {
    // Check if user is logged in
    const token = localStorage.getItem('token');
    if (!token) {
        return; // Not logged in, don't show friends menu items
    }

    // Show the friends menu items for logged in users
    const friendsItem = document.getElementById('friendsItem');
    const friendSetsItem = document.getElementById('friendSetsItem');
    
    if (friendsItem) friendsItem.style.display = 'block';
    if (friendSetsItem) friendSetsItem.style.display = 'block';
    
    // Check for pending friend requests if we're not on the friends page
    if (!window.location.pathname.includes('/FlashcardsView/Friends')) {
        checkPendingFriendRequests();
    }
    
    // Function to check for pending friend requests
    function checkPendingFriendRequests() {
        const userInfo = window.getUserInfoFromToken();
        if (!userInfo) return;
        
        console.log("Checking friend requests for user:", userInfo.userId);
        
        // Call API to check pending friend requests
        fetch('/api/friends/pending-requests', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to fetch pending friend requests');
            }
            return response.json();
        })
        .then(data => {
            // If there are pending requests, show notification badge
            if (data.pendingCount > 0) {                    // Create notification badge on friends menu item
                    const friendsLink = friendsItem.querySelector('a');
                    if (friendsLink) {
                        // Check if badge already exists
                        let badge = friendsLink.querySelector('.badge');
                        if (!badge) {
                            badge = document.createElement('span');
                            badge.className = 'badge rounded-pill bg-danger ms-2 pulse-animation';
                            friendsLink.appendChild(badge);
                        }
                        // Update badge text
                        badge.textContent = data.pendingCount;
                        console.log(`Added notification badge: ${data.pendingCount} pending friend requests`);
                        
                        // Update page title if there are pending requests (like: (3) Flashcard App)
                        const originalTitle = document.title;
                        if (!originalTitle.startsWith(`(${data.pendingCount})`)) {
                            document.title = `(${data.pendingCount}) ${originalTitle.replace(/^\(\d+\) /, '')}`;
                        }
                    }
            }
        })
        .catch(error => {
            console.error('Error checking friend requests:', error);
        });
    }
    
    // Re-check for pending requests periodically
    if (!window.location.pathname.includes('/FlashcardsView/Friends')) {
        setInterval(checkPendingFriendRequests, 60000); // Check every minute
    }
});
