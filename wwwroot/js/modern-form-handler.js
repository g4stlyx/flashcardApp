// Create/Edit Page Handler

document.addEventListener("DOMContentLoaded", function() {
    if (window.location.pathname.includes("/FlashcardsView/Create") || 
        window.location.pathname.includes("/FlashcardsView/Edit")) {
        
        initializeModernForm();
    }
    
    function initializeModernForm() {
        console.log("Initializing modern form");
        
        // Cover image preview functionality
        const coverImageUrl = document.getElementById('coverImageUrl');
        const coverPreviewImage = document.getElementById('coverPreviewImage');
        const coverPreviewPlaceholder = document.getElementById('coverPreviewPlaceholder');
        
        if (coverImageUrl && coverPreviewImage) {
            // Initial check in case there's already a URL
            updateCoverImagePreview();
            
            // Add event listener for URL changes
            coverImageUrl.addEventListener('input', updateCoverImagePreview);
            
            function updateCoverImagePreview() {
                const url = coverImageUrl.value.trim();
                if (url) {
                    coverPreviewImage.src = url;
                    coverPreviewImage.classList.remove('d-none');
                    coverPreviewPlaceholder.classList.add('d-none');
                    
                    // Check if image loads successfully
                    coverPreviewImage.onerror = function() {
                        coverPreviewImage.classList.add('d-none');
                        coverPreviewPlaceholder.classList.remove('d-none');
                        coverPreviewPlaceholder.innerHTML = `
                            <i class="bi bi-exclamation-triangle text-warning" style="font-size: 4rem;"></i>
                            <p class="text-warning mt-2">Görsel yüklenemedi. URL'i kontrol ediniz.</p>
                        `;
                    };
                    
                    coverPreviewImage.onload = function() {
                        // Reset any previous error state
                        coverPreviewPlaceholder.innerHTML = `
                            <i class="bi bi-card-image text-muted" style="font-size: 4rem;"></i>
                            <p class="text-muted mt-2">Kapak fotoğrafı önizlemesi</p>
                        `;
                    };
                } else {
                    coverPreviewImage.classList.add('d-none');
                    coverPreviewPlaceholder.classList.remove('d-none');
                }
            }
        }
        
        // Radio button visibility syncing with hidden select
        const visibilityRadios = document.querySelectorAll('input[name="visibilityRadio"]');
        const visibilitySelect = document.getElementById('visibility');
        
        if (visibilityRadios.length && visibilitySelect) {
            // Set initial state from select to radios if needed (for edit page)
            const currentVisibility = visibilitySelect.value;
            visibilityRadios.forEach(radio => {
                if (radio.value === currentVisibility) {
                    radio.checked = true;
                }
            });
            
            // Radio change event
            visibilityRadios.forEach(radio => {
                radio.addEventListener('change', function() {
                    if (this.checked) {
                        visibilitySelect.value = this.value;
                    }
                });
            });
        }
        
        // Visual styling for visibility options
        const visibilityOptions = document.querySelectorAll('.visibility-option');
        
        if (visibilityOptions.length) {
            visibilityOptions.forEach(option => {
                const radio = option.querySelector('input[type="radio"]');
                const label = option.querySelector('label');
                
                // Set initial styles
                updateVisibilityOptionStyle(radio, label);
                
                // Listen for changes
                radio.addEventListener('change', function() {
                    // Reset all
                    visibilityOptions.forEach(opt => {
                        const r = opt.querySelector('input[type="radio"]');
                        const l = opt.querySelector('label');
                        updateVisibilityOptionStyle(r, l);
                    });
                });
            });
            
            function updateVisibilityOptionStyle(radio, label) {
                if (radio.checked) {
                    label.classList.add('border-primary', 'bg-light');
                    label.classList.remove('border-secondary');
                } else {
                    label.classList.remove('border-primary', 'bg-light');
                    label.classList.add('border-secondary');
                }
            }
        }
        
        // Add form validation
        const form = document.getElementById('createSetForm');
        if (form) {
            form.addEventListener('submit', function(event) {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                    
                    // Find the first invalid element and focus it
                    const invalidElement = form.querySelector(':invalid');
                    if (invalidElement) {
                        invalidElement.focus();
                    }
                    
                    // Show validation messages
                    form.classList.add('was-validated');
                }
            });
        }
        
        // Focus first input on page load
        const firstInput = document.getElementById('title');
        if (firstInput) {
            setTimeout(() => firstInput.focus(), 200);
        }
    }
});
