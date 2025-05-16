// Special handler for Set.cshtml page
document.addEventListener("DOMContentLoaded", function () {
  console.log("Set page handler loaded");

  // Check if we're on a Set page
  if (window.location.pathname.includes("/FlashcardsView/Set/")) {
    console.log("Set page detected, setting up protected links");

    // Get the JWT token
    const token = localStorage.getItem("token");
    console.log("Token available:", token ? "Yes" : "No");

    // Function to handle protected link clicks
    function handleSetPageLink(e) {
      e.preventDefault();
      const href = this.getAttribute("href");
      console.log("Intercepted click on protected link:", href);

      // If no token, let the default login flow happen
      if (!token) {
        console.log("No token, redirecting to login");
        window.location.href = "/AuthView/Login";
        return;
      }

      // For Study links, construct the URL with token
      if (href.includes("/FlashcardsView/Study/")) {
        const separator = href.includes("?") ? "&" : "?";
        const studyUrl =
          href + separator + "token=" + encodeURIComponent(token);
        console.log("Redirecting to study with token:", studyUrl);
        window.location.href = studyUrl;
        return;
      }

      // For Edit links, construct the URL with token
      if (href.includes("/FlashcardsView/Edit/")) {
        const separator = href.includes("?") ? "&" : "?";
        const editUrl = href + separator + "token=" + encodeURIComponent(token);
        console.log("Redirecting to edit with token:", editUrl);
        window.location.href = editUrl;
        return;
      }

      // Fallback - use the generic handler
      console.log("Using general token handler for:", href);
      if (typeof handleProtectedLink === "function") {
        handleProtectedLink(href);
      } else {
        console.error(
          "handleProtectedLink function not found, using direct navigation with token"
        );
        const separator = href.includes("?") ? "&" : "?";
        window.location.href =
          href + separator + "token=" + encodeURIComponent(token);
      }
    }

    // Find all Study links on the page
    const studyLinks = document.querySelectorAll(
      'a[href*="/FlashcardsView/Study/"]'
    );
    studyLinks.forEach((link) => {
      console.log("Adding handler to Study link:", link.getAttribute("href"));
      // Remove the default handler if it exists
      if (link.getAttribute("data-set-handler-attached") !== "true") {
        link.addEventListener("click", handleSetPageLink);
        // Mark as processed
        link.setAttribute("data-set-handler-attached", "true");
      }
    });

    // Find all Edit links on the page
    const editLinks = document.querySelectorAll(
      'a[href*="/FlashcardsView/Edit/"]'
    );
    editLinks.forEach((link) => {
      console.log("Adding handler to Edit link:", link.getAttribute("href"));
      // Remove the default handler if it exists
      if (link.getAttribute("data-set-handler-attached") !== "true") {
        link.addEventListener("click", handleSetPageLink);
        // Mark as processed
        link.setAttribute("data-set-handler-attached", "true");
      }
    });
  }
});
