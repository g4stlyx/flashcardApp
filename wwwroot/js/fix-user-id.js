// Script to fix user ID issues by providing manual control

document.addEventListener("DOMContentLoaded", function() {
    // Only add this to the debug pages
    if (window.location.pathname.includes("UserSetsDebug") || window.location.pathname.includes("AuthDebug")) {
        console.log("User ID fix tool enabled");
        
        // Add the ID fix form if it doesn't exist yet
        if (!document.getElementById("fixUserIdForm")) {
            createFixUserIdForm();
        }
    }
});

// Create a form for manually setting the user ID
function createFixUserIdForm() {
    // Create the HTML for the form
    const formHtml = `
    <div id="fixUserIdForm" class="card mb-4">
        <div class="card-header bg-warning text-dark">
            <h2>Manual User ID Fix</h2>
        </div>
        <div class="card-body">
            <p>Use this to manually set the user ID in cookies and localStorage to fix authentication issues.</p>
            <div class="mb-3">
                <label for="manualUserId" class="form-label">User ID:</label>
                <input type="number" class="form-control" id="manualUserId" placeholder="Enter user ID">
            </div>
            <div class="mb-3">
                <label for="username" class="form-label">Username (optional):</label>
                <input type="text" class="form-control" id="username" placeholder="Enter username">
            </div>
            <button id="applyUserId" class="btn btn-warning">Apply User ID</button>
            <div id="applyResult" class="mt-3"></div>
        </div>
    </div>`;
    
    // Add the form to the page
    const container = document.querySelector(".container");
    if (container) {
        const div = document.createElement("div");
        div.innerHTML = formHtml;
        container.appendChild(div);
        
        // Add event listener
        document.getElementById("applyUserId").addEventListener("click", function() {
            const userId = document.getElementById("manualUserId").value;
            const username = document.getElementById("username").value;
            
            if (!userId || isNaN(parseInt(userId, 10))) {
                document.getElementById("applyResult").innerHTML = '<div class="alert alert-danger">Please enter a valid User ID</div>';
                return;
            }
            
            // Apply the user ID to cookies
            document.cookie = `user_id=${userId}; path=/; max-age=${60*60*24*7}`; // 7 days
            
            // Apply to localStorage
            localStorage.setItem("userId", userId);
            
            // If we have a token, update it with the new user ID
            const token = localStorage.getItem("token");
            if (token) {
                try {
                    // Parse token to get all claims
                    const base64Url = token.split('.')[1];
                    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
                    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
                    }).join(''));

                    const claims = JSON.parse(jsonPayload);
                    
                    // Log the token issues
                    console.log("Original token user ID:", claims.nameid);
                    console.log("Setting to new user ID:", userId);
                    
                } catch (e) {
                    console.error("Error parsing token:", e);
                }
            }
            
            // Success message
            document.getElementById("applyResult").innerHTML = `
                <div class="alert alert-success">
                    <p>User ID ${userId} applied to cookies and localStorage.</p>
                    <p>You should now reload and test the MySets page with this URL:</p>
                    <a href="/FlashcardsView/MySets?userId=${userId}" class="btn btn-sm btn-primary">
                        Go to MySets with User ID ${userId}
                    </a>
                </div>`;
        });
    }
}
