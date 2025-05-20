// Enhanced Set page handler with modern UI interactions

document.addEventListener('DOMContentLoaded', function() {
    // View switching functionality
    const gridViewBtn = document.getElementById('gridViewBtn');
    const listViewBtn = document.getElementById('listViewBtn');
    const flashcardItems = document.querySelectorAll('.flashcard-item');
    
    if (gridViewBtn && listViewBtn) {
        // Grid view (default)
        gridViewBtn.addEventListener('click', function() {
            gridViewBtn.classList.add('active');
            listViewBtn.classList.remove('active');
            
            flashcardItems.forEach(item => {
                item.classList.remove('col-12', 'flashcard-list-view');
                item.classList.add('col-md-6');
                
                // Reset any inline styles we might have added for list view
                const flashcardFront = item.querySelector('.flashcard-front');
                if (flashcardFront) {
                    flashcardFront.style.justifyContent = '';
                }
            });
            
            localStorage.setItem('flashcardViewPreference', 'grid');
        });
        // List view
        listViewBtn.addEventListener('click', function() {
            listViewBtn.classList.add('active');
            gridViewBtn.classList.remove('active');
            
            flashcardItems.forEach(item => {
                item.classList.remove('col-md-6');
                item.classList.add('col-12');
                item.classList.add('flashcard-list-view');
                
                // Reorganize content for list view
                const termElement = item.querySelector('.flashcard-front h4');
                const imageContainer = item.querySelector('.card-image-container');
                
                if (termElement && imageContainer) {
                    const flashcardFront = item.querySelector('.flashcard-front');
                    // Adjust layout for better list view experience
                    if (flashcardFront) {
                        flashcardFront.style.justifyContent = 'space-between';
                    }
                }
            });
            
            localStorage.setItem('flashcardViewPreference', 'list');
        });
        
        // Load saved preference
        const savedViewPreference = localStorage.getItem('flashcardViewPreference');
        if (savedViewPreference === 'list') {
            listViewBtn.click();
        }
    }
      // Search functionality
    const searchContainer = document.createElement('div');
    searchContainer.className = 'search-container mt-3 mb-4';
    
    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.className = 'form-control';
    searchInput.placeholder = 'Kartlarda ara...';
    searchInput.setAttribute('aria-label', 'Kart ara');
    
    // Add search icon
    const searchInputGroup = document.createElement('div');
    searchInputGroup.className = 'input-group';
    
    const searchIcon = document.createElement('span');
    searchIcon.className = 'input-group-text bg-white';
    searchIcon.innerHTML = '<i class="bi bi-search"></i>';
    
    searchInputGroup.appendChild(searchIcon);
    searchInputGroup.appendChild(searchInput);
    searchContainer.appendChild(searchInputGroup);
    
    const flashcardsContainer = document.querySelector('.col-lg-8 .cards-header');
    flashcardsContainer.parentNode.insertBefore(searchContainer, flashcardsContainer.nextSibling);
    
    searchInput.addEventListener('input', function(e) {
        const searchTerm = e.target.value.toLowerCase().trim();
        
        flashcardItems.forEach(item => {
            const term = item.querySelector('.flashcard-front h4').textContent.toLowerCase();
            const definition = item.querySelector('.flashcard-back p').textContent.toLowerCase();
            
            if (term.includes(searchTerm) || definition.includes(searchTerm)) {
                item.style.display = '';
            } else {
                item.style.display = 'none';
            }
        });
    });
    
    // Flashcards animation
    const flashcards = document.querySelectorAll('.flashcard');
    flashcards.forEach(card => {
        card.addEventListener('click', function() {
            this.classList.toggle('flipped');
        });
        
        // Add hover effect
        card.addEventListener('mouseenter', function() {
            this.style.transform = this.classList.contains('flipped') ? 
                'rotateY(180deg) translateY(-5px)' : 'translateY(-5px)';
            this.style.boxShadow = 'var(--shadow-lg)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = this.classList.contains('flipped') ? 
                'rotateY(180deg)' : '';
            this.style.boxShadow = '';
        });
    });
    
    // Favorite button functionality
    const favoriteButton = document.getElementById('favoriteButton');
    if (favoriteButton) {
        favoriteButton.addEventListener('click', function() {
            const setId = window.location.pathname.split('/').pop();
            const token = localStorage.getItem('token');
            
            if (!token) {
                alert('Favorilere eklemek için giriş yapmalısınız.');
                return;
            }
            
            // Toggle favorite status
            if (this.classList.contains('favorited')) {
                // Remove from favorites
                this.classList.remove('favorited', 'btn-danger');
                this.classList.add('btn-outline-secondary');
                this.innerHTML = '<i class="bi bi-heart me-2"></i>Favorilere Ekle';
                
                // API call would go here
                console.log('Removing from favorites');
            } else {
                // Add to favorites
                this.classList.add('favorited', 'btn-danger');
                this.classList.remove('btn-outline-secondary');
                this.innerHTML = '<i class="bi bi-heart-fill me-2"></i>Favorilerden Çıkar';
                
                // API call would go here
                console.log('Adding to favorites');
            }
        });
    }

    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });
});
