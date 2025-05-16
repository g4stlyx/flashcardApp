// Special handler for Study page
document.addEventListener("DOMContentLoaded", function() {
    console.log("Study page handler loaded - NEW VERSION");
    
    // Check if we're on a Study page
    if (window.location.pathname.includes("/FlashcardsView/Study/")) {
        console.log("Study page detected, initializing flashcard study interface");
        
        // Get token
        const token = localStorage.getItem("token");
        if (!token) {
            console.log("No token found for Study page, redirecting to login");
            window.location.href = "/AuthView/Login";
            return;
        }
        
        // Check if the window.flashcards is available, if not try to wait a bit more
        if (!window.flashcards) {
            console.log("window.flashcards not available immediately, waiting for it to load...");
            let attempts = 0;
            const maxAttempts = 10;
            
            // Poll for flashcards availability with increasing timeouts
            function pollForFlashcards() {
                if (window.flashcards) {
                    console.log("Flashcards loaded after polling");
                    initializeStudy();
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
            setTimeout(initializeStudy, 200);
        }
        
        function showLoadingError() {
            $("#cardFront").html("<p class='text-danger'>Kart verileri yüklenemedi. Lütfen sayfayı yenileyin.</p>");
            $("#flipBtn, #nextBtn, #prevBtn, #shuffleBtn").prop("disabled", true);
        }
        
        function initializeStudy() {
            console.log("Initializing study page functionality");
            
            // Debug to see what's in the window.flashcards
            if (window.flashcards) {
                console.log("Found flashcards in window object:", window.flashcards);
            } else {
                console.error("No flashcards found in window object!");
            }
            
            // Make sure we have valid data
            const flashcards = window.flashcards || [];
            let currentIndex = 0;
            let isFlipped = false;
            
            // Ensure jQuery is available before continuing
            if (typeof $ === 'undefined') {
                console.error("jQuery not available on Study page!");
                return;
            }
            
            // Update the display (both UI state and content)
            if (flashcards.length === 0) {
                console.error("No flashcards available!");
                $("#cardFront").html("<p class='text-danger'>Kart bulunamadı!</p>");
                $("#flipBtn, #nextBtn, #prevBtn, #shuffleBtn").prop("disabled", true);
                return;
            }
            
            // Handle and fix JSON escaping issues
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
            
            // Function to display a card at given index
            function displayCard(index) {
                console.log("Displaying card at index:", index);
                const card = flashcards[index];
                
                // Update card counts
                $("#currentCardIndex").text(index + 1);
                $("#totalCards").text(flashcards.length);
                
                // Front side
                $("#termText").text(card.term || "");
                
                // Back side
                $("#definitionText").text(card.definition || "");
                
                // Example sentence (if available)
                if (card.exampleSentence && card.exampleSentence !== "null") {
                    $("#exampleText").text("Örnek: " + card.exampleSentence);
                    $("#exampleText").show();
                } else {
                    $("#exampleText").hide();
                }
                
                // Image (if available)
                if (card.imageUrl && card.imageUrl !== "null") {
                    $("#cardImage").attr("src", card.imageUrl);
                    $("#imageContainer").show();
                } else {
                    $("#imageContainer").hide();
                }
                
                // Show front side by default
                isFlipped = false;
                $("#cardFront").css("display", "flex");
                $("#cardBack").hide();
                $("#flipBtn").text("Kartı çevir");
                
                // Enable/disable navigation buttons as needed
                updateNavButtons();
            }
            
            // Function to flip the current card
            function flipCard() {
                console.log("Flipping card, current state: ", isFlipped ? "showing back" : "showing front");
                
                if (!isFlipped) {
                    // Show back side
                    $("#cardFront").hide();
                    $("#cardBack").css({
                        "display": "flex",
                        "flex-direction": "column",
                        "justify-content": "center",
                        "align-items": "center",
                        "height": "100%"
                    });
                    $("#flipBtn").text("Terimi göster");
                } else {
                    // Show front side
                    $("#cardBack").hide();
                    $("#cardFront").css({
                        "display": "flex",
                        "flex-direction": "column",
                        "justify-content": "center",
                        "align-items": "center",
                        "height": "100%"
                    });
                    $("#flipBtn").text("Kartı çevir");
                }
                
                isFlipped = !isFlipped;
            }
            
            // Function to update navigation button states
            function updateNavButtons() {
                $("#prevBtn").prop("disabled", currentIndex === 0);
                $("#nextBtn").prop("disabled", currentIndex === flashcards.length - 1);
            }
            
            // Function to shuffle flashcards
            function shuffleFlashcards() {
                for (let i = flashcards.length - 1; i > 0; i--) {
                    const j = Math.floor(Math.random() * (i + 1));
                    [flashcards[i], flashcards[j]] = [flashcards[j], flashcards[i]];
                }
                currentIndex = 0;
                displayCard(currentIndex);
            }
            
            // Remove any existing event handlers to prevent duplicates
            $("#flipBtn, #nextBtn, #prevBtn, #shuffleBtn").off("click");
            
            // Set up all event handlers
            $("#flipBtn").on("click", function() {
                console.log("Flip button clicked");
                flipCard();
            });
            
            $("#nextBtn").on("click", function() {
                console.log("Next button clicked");
                if (currentIndex < flashcards.length - 1) {
                    currentIndex++;
                    displayCard(currentIndex);
                }
            });
            
            $("#prevBtn").on("click", function() {
                console.log("Previous button clicked");
                if (currentIndex > 0) {
                    currentIndex--;
                    displayCard(currentIndex);
                }
            });
            
            $("#shuffleBtn").on("click", function() {
                console.log("Shuffle button clicked");
                shuffleFlashcards();
            });
            
            // Initialize with first card
            displayCard(0);
            console.log("Study page initialization complete");
        }
    }
});
