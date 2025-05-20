// handles the deletion of flashcard sets by owners and admins

document.addEventListener('DOMContentLoaded', function() {
    setupDeleteSetButtons();
});

function setupDeleteSetButtons() {
    console.log('Setting up delete set functionality');
    
    // Check if we're on the set view page
    const setDetails = document.querySelector('.card-title');
    const setId = getSetIdFromUrl();
    
    if (!setId) {
        console.log('No set ID found in URL, skipping delete button setup');
        return;
    }

    console.log(`Set ID detected: ${setId}`);
    
    // Get JWT token from localStorage
    const token = localStorage.getItem('token');
    if (!token) {
        console.log('No token found, user not logged in');
        return;
    }
    
    try {
        // Parse the JWT token payload
        const tokenPayload = JSON.parse(atob(token.split('.')[1]));
        
        // Get user ID from token
        const currentUserId = parseInt(tokenPayload.nameid);
        
        // Get set owner ID from the page if available
        const setOwnerId = document.body.getAttribute('data-set-owner-id') || 
                          document.querySelector('[data-set-owner-id]')?.getAttribute('data-set-owner-id');
        
        // Check if user is admin
        const isAdmin = tokenPayload.role === "Admin" || 
                        tokenPayload["UserType"] === "Admin" ||
                        (tokenPayload.role && Array.isArray(tokenPayload.role) && tokenPayload.role.includes("Admin"));
        
        console.log(`Current user: ${currentUserId}, Set owner: ${setOwnerId}, Is admin: ${isAdmin}`);
        
        // If user is owner or admin, add delete button
        if ((setOwnerId && parseInt(setOwnerId) === currentUserId) || isAdmin) {
            addDeleteButton(setId, token);
        }
    } catch (err) {
        console.error('Error setting up delete functionality:', err);
    }
}

function addDeleteButton(setId, token) {
    // Find the button container in the set view page
    const buttonContainer = document.querySelector('.card-body .d-grid');
    
    if (!buttonContainer) {
        console.error('Button container not found');
        return;
    }
    
    console.log('Adding delete button to set view page');
    
    // Create delete button
    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'btn btn-danger mt-2';
    deleteButton.id = 'deleteSetButton';
    deleteButton.textContent = 'Seti Sil';
    
    // Add the delete button to the container
    buttonContainer.appendChild(deleteButton);
    
    // Add click event listener
    deleteButton.addEventListener('click', function() {
        // Show confirmation dialog
        if (confirm('Bu seti silmek istediğinizden emin misiniz? Bu işlem geri alınamaz ve tüm kartlar silinecektir.')) {
            deleteSet(setId, token);
        }
    });
}

function deleteSet(setId, token) {
    console.log(`Deleting set ID: ${setId}`);
    
    // Make API call to delete the set
    fetch(`/api/flashcard-sets/${setId}`, {
        method: 'DELETE',
        headers: {
            'Authorization': `Bearer ${token}`
        }
    })
    .then(response => {
        if (response.ok || response.status === 204) {
            console.log('Set deleted successfully');
            
            // Show success message and redirect
            alert('Set başarıyla silindi!');
            
            // Redirect to MySets page
            if (typeof navigateWithToken === 'function') {
                navigateWithToken('/FlashcardsView/MySets');
            } else {
                window.location.href = '/FlashcardsView/MySets?token=' + encodeURIComponent(token);
            }
        } else {
            response.text().then(text => {
                console.error('Error deleting set:', text);
                alert('Set silinirken bir hata oluştu: ' + text);
            });
        }
    })
    .catch(error => {
        console.error('Error deleting set:', error);
        alert('Set silinirken bir hata oluştu');
    });
}

function getSetIdFromUrl() {
    // Extract the set ID from the URL
    // URL format: /FlashcardsView/Set/123
    const path = window.location.pathname;
    const pathParts = path.split('/');
    
    // Check if we're on a set page or edit page
    if ((pathParts.length >= 3 && pathParts[1].toLowerCase() === 'flashcardsview') && 
        (pathParts[2].toLowerCase() === 'set' || pathParts[2].toLowerCase() === 'edit') && 
        pathParts[3]) {
            return pathParts[3];
    }
    
    return null;
}
