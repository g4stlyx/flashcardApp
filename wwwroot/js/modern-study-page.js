// Modern Study Page Handler with Enhanced interactive flashcard study experience

document.addEventListener("DOMContentLoaded", function() {
    console.log("Modern study page handler loaded");
    
    // Check if we're on a Study page
    if (window.location.pathname.includes("/FlashcardsView/Study/")) {
        console.log("Modern study page detected, initializing enhanced flashcard study interface");
        
        // Debug available elements
        console.log("Checking for elements:");
        console.log("flipBtn exists:", $("#flipBtn").length > 0);
        console.log("shuffleBtn exists:", $("#shuffleBtn").length > 0);
        console.log("resetBtn exists:", $("#resetBtn").length > 0);
        console.log("autoplayBtn exists:", $("#autoplayBtn").length > 0);
        
        // Get token - but don't redirect if missing to allow for public sets
        const token = localStorage.getItem("token");
        
        // Check if the window.flashcards is available
        if (!window.flashcards) {
            console.log("window.flashcards not available immediately, waiting for it to load...");
            let attempts = 0;
            const maxAttempts = 10;
            
            // Poll for flashcards availability with increasing timeouts
            function pollForFlashcards() {
                if (window.flashcards) {
                    console.log("Flashcards loaded after polling");
                    initializeModernStudy();
                } else if (attempts < maxAttempts) {
                    attempts++;
                    console.log(`Attempt ${attempts}/${maxAttempts} to find flashcards, waiting...`);
                    setTimeout(pollForFlashcards, 200 * attempts);
                } else {
                    console.error("Failed to load flashcards after multiple attempts");
                    showLoadingError();
                }
            }
            
            pollForFlashcards();
        } else {
            // Flashcards already available, initialize immediately
            setTimeout(initializeModernStudy, 200);
        }
        
        function showLoadingError() {
            $("#cardFront").html(`
                <div class="text-center">
                    <i class="bi bi-exclamation-circle text-danger" style="font-size: 3rem;"></i>
                    <p class="text-danger mt-3">Kart verileri yüklenemedi. Lütfen sayfayı yenileyin.</p>
                </div>
            `);
            $("#flipBtn, #nextBtn, #prevBtn, #shuffleBtn, #resetBtn, #autoplayBtn").prop("disabled", true);
        }
        
        function initializeModernStudy() {
            console.log("Initializing modern study page functionality");
            
            // Debug to see what's in the window.flashcards
            if (window.flashcards) {
                console.log("Found flashcards in window object:", window.flashcards);
            } else {
                console.error("No flashcards found in window object!");
                return;
            }
            
            // Make sure we have valid data
            const flashcards = window.flashcards || [];
            let currentIndex = 0;
            let isFlipped = false;
            let isAutoplayActive = false;
            let autoplayInterval;
            const autoplaySpeed = 3000; // milliseconds
            
            // Fix JSON escaping issues
            function fixJsonString(str) {
                if (!str) return "";
                
                // If string has quotes at beginning and end, remove them
                str = str.replace(/^"|"$/g, '');
                
                // Replace escaped quotes with actual quotes
                str = str.replace(/\\"/g, '"');
                
                // Replace other common escaped characters
                str = str.replace(/\\n/g, '\n')
                         .replace(/\\r/g, '')
                         .replace(/\\t/g, '\t');
                
                return str;
            }
            
            // Fix all flashcard text fields
            flashcards.forEach(card => {
                if (card.term) card.term = fixJsonString(card.term);
                if (card.definition) card.definition = fixJsonString(card.definition);
                if (card.exampleSentence) card.exampleSentence = fixJsonString(card.exampleSentence);
                if (card.imageUrl) card.imageUrl = fixJsonString(card.imageUrl);
            });
            
            // Update the flashcard content and state
            function updateCard() {
                const card = flashcards[currentIndex];
                if (!card) {
                    console.error(`Invalid card at index ${currentIndex}`);
                    return;
                }
                
                // Update term (front)
                $("#termText").text(card.term || "");
                // Update image if available
                if (card.imageUrl && card.imageUrl.trim() !== "") {
                    const imageElement = $("#cardImage");
                    
                    // Add loading indicator
                    $("#imageContainer").html(`
                        <div class="image-loading-indicator">
                            <div class="spinner-border text-primary spinner-border-sm" role="status">
                                <span class="visually-hidden">Loading...</span>
                            </div>
                        </div>
                        <img id="cardImage" class="img-fluid rounded" style="display: none;">
                    `);
                    
                    // Get fresh reference to image element
                    const newImageElement = $("#cardImage");
                    
                    // Set up load/error handlers before setting src
                    newImageElement.on("load", function() {
                        // Remove loading indicator and show image
                        $(".image-loading-indicator").remove();
                        $(this).fadeIn(200);
                    }).on("error", function() {
                        $("#imageContainer").html(`
                            <div class="text-danger">
                                <i class="bi bi-exclamation-triangle"></i>
                                <small>Resim yüklenemedi</small>
                            </div>
                        `);
                    });
                    
                    // Set the source
                    newImageElement.attr("src", card.imageUrl);
                    
                    // Make sure the image container is visible and has the right classes
                    $("#imageContainer").removeClass("d-none").addClass("d-block image-section");
                } else {
                    $("#imageContainer").removeClass("d-block image-section").addClass("d-none");
                }
                
                // Update definition (back)
                $("#definitionText").text(card.definition || "");
                
                // Update example if available
                if (card.exampleSentence) {
                    $("#exampleText").text(card.exampleSentence);
                    $("#exampleContainer").removeClass("d-none").addClass("d-block");
                } else {
                    $("#exampleContainer").removeClass("d-block").addClass("d-none");
                }
                
                // Reset flip state
                isFlipped = false;
                updateFlipState();
                
                // Update UI indicators
                $("#currentCardIndex").text(currentIndex + 1);
                
                // Update navigation buttons
                $("#prevBtn").prop("disabled", currentIndex === 0);
                $("#nextBtn").prop("disabled", currentIndex === flashcards.length - 1);
                
                // Update progress bar
                const progressPercentage = ((currentIndex + 1) / flashcards.length) * 100;
                $("#studyProgress").css("width", `${progressPercentage}%`);
                
                // Save current position in session storage
                sessionStorage.setItem("currentCardIndex", currentIndex);
            }
            
            // Update the visual state for flipped/unflipped card
            function updateFlipState() {
                const flashcardElement = document.getElementById("flashcard");
                
                if (isFlipped) {
                    $("#cardFront").addClass("d-none").removeClass("d-flex");
                    $("#cardBack").removeClass("d-none").addClass("d-flex");
                    if (flashcardElement) flashcardElement.classList.add("flipped");
                } else {
                    $("#cardFront").removeClass("d-none").addClass("d-flex");
                    $("#cardBack").addClass("d-none").removeClass("d-flex");
                    if (flashcardElement) flashcardElement.classList.remove("flipped");
                }
            }
              // Set up direct event listener references to avoid jQuery binding issues
            const flipBtn = document.getElementById("flipBtn");
            const flashcardElement = document.getElementById("flashcard");
            const prevBtn = document.getElementById("prevBtn");
            const nextBtn = document.getElementById("nextBtn");
            const shuffleBtn = document.getElementById("shuffleBtn");
            const resetBtn = document.getElementById("resetBtn");
            const autoplayBtn = document.getElementById("autoplayBtn");
            
            console.log("Direct DOM element checks:");
            console.log("flipBtn:", flipBtn ? "found" : "not found");
            console.log("shuffleBtn:", shuffleBtn ? "found" : "not found");
            console.log("resetBtn:", resetBtn ? "found" : "not found");
            console.log("autoplayBtn:", autoplayBtn ? "found" : "not found");
            
            // Handle flip button
            if (flipBtn) {
                flipBtn.addEventListener("click", function(e) {
                    console.log("Flip button clicked");
                    e.preventDefault(); // Prevent any default behavior
                    isFlipped = !isFlipped;
                    updateFlipState();
                });
            }
            
            // Handle clicking the card itself to flip
            if (flashcardElement) {
                flashcardElement.addEventListener("click", function() {
                    console.log("Card clicked for flip");
                    isFlipped = !isFlipped;
                    updateFlipState();
                });
            }
            
            // Handle previous button
            if (prevBtn) {
                prevBtn.addEventListener("click", function() {
                    console.log("Previous button clicked");
                    if (currentIndex > 0) {
                        currentIndex--;
                        updateCard();
                    }
                });
            }
            
            // Handle next button
            if (nextBtn) {
                nextBtn.addEventListener("click", function() {
                    console.log("Next button clicked");
                    if (currentIndex < flashcards.length - 1) {
                        currentIndex++;
                        updateCard();
                    }
                });
            }
            
            // Handle shuffle button
            if (shuffleBtn) {
                shuffleBtn.addEventListener("click", function() {
                    console.log("Shuffle button clicked");
                    // Stop autoplay if active
                    stopAutoplay();
                    
                    // Fisher-Yates shuffle algorithm
                    for (let i = flashcards.length - 1; i > 0; i--) {
                        const j = Math.floor(Math.random() * (i + 1));
                        [flashcards[i], flashcards[j]] = [flashcards[j], flashcards[i]];
                    }
                    
                    // Reset to first card
                    currentIndex = 0;
                    updateCard();
                    
                    // Show feedback
                    const toastContainer = document.createElement("div");
                    toastContainer.className = "position-fixed top-0 end-0 p-3";
                    toastContainer.style.zIndex = "1050";
                    
                    toastContainer.innerHTML = `
                        <div class="toast align-items-center text-white bg-primary border-0" role="alert" aria-live="assertive" aria-atomic="true">
                            <div class="d-flex">
                                <div class="toast-body">
                                    <i class="bi bi-shuffle me-2"></i>Kartlar karıştırıldı
                                </div>
                                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                            </div>
                        </div>
                    `;
                    
                    document.body.appendChild(toastContainer);
                    const toastElement = toastContainer.querySelector('.toast');
                    const bsToast = new bootstrap.Toast(toastElement);
                    bsToast.show();
                    
                    // Remove the toast after it's hidden
                    setTimeout(() => {
                        if (toastContainer.parentNode) {
                            toastContainer.parentNode.removeChild(toastContainer);
                        }
                    }, 3000);
                });
            }
            
            // Handle reset button
            if (resetBtn) {
                resetBtn.addEventListener("click", function() {
                    console.log("Reset button clicked");
                    // Stop autoplay if active
                    stopAutoplay();
                    
                    // Reset to first card
                    currentIndex = 0;
                    updateCard();
                });
            }
            
            // Autoplay functionality
            function toggleAutoplay() {
                console.log("Toggling autoplay");
                if (isAutoplayActive) {
                    stopAutoplay();
                } else {
                    startAutoplay();
                }
            }
            
            function startAutoplay() {
                isAutoplayActive = true;
                if (autoplayBtn) {
                    autoplayBtn.innerHTML = '<i class="bi bi-pause-fill"></i>';
                    autoplayBtn.classList.add("btn-success");
                    autoplayBtn.classList.remove("btn-outline-success");
                }
                
                autoplayInterval = setInterval(function() {
                    if (!isFlipped) {
                        isFlipped = true;
                        updateFlipState();
                    } else {
                        isFlipped = false;
                        if (currentIndex < flashcards.length - 1) {
                            currentIndex++;
                        } else {
                            currentIndex = 0; // Loop back to beginning
                        }
                        updateCard();
                    }
                }, autoplaySpeed);
            }
            
            function stopAutoplay() {
                if (autoplayInterval) {
                    clearInterval(autoplayInterval);
                }
                isAutoplayActive = false;
                if (autoplayBtn) {
                    autoplayBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
                    autoplayBtn.classList.remove("btn-success");
                    autoplayBtn.classList.add("btn-outline-success");
                }
            }
            
            if (autoplayBtn) {
                autoplayBtn.addEventListener("click", toggleAutoplay);
            }
            
            // Keyboard navigation
            $(document).on("keydown", function(e) {
                // Only if focus is not in an input field
                if (document.activeElement.tagName !== "INPUT" && document.activeElement.tagName !== "TEXTAREA") {
                    switch(e.which) {
                        case 37: // left arrow
                            $("#prevBtn").click();
                            break;
                        case 39: // right arrow
                            $("#nextBtn").click();
                            break;
                        case 32: // space
                            e.preventDefault();
                            $("#flipBtn").click();
                            break;
                        case 80: // p key for play/pause
                            toggleAutoplay();
                            break;
                    }
                }
            });
            
            // Check if there's a saved position
            const savedIndex = sessionStorage.getItem("currentCardIndex");
            if (savedIndex !== null && !isNaN(parseInt(savedIndex))) {
                currentIndex = Math.min(parseInt(savedIndex), flashcards.length - 1);
            }
            
            // Initialize first card
            updateCard();
            
            // Bootstrap tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
            var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl)
            });
        }
    }
});
