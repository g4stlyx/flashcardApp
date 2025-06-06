window.checkUserIdMatch = function() {
  const token = localStorage.getItem("token");
  if (!token) return false;

  try {
    const claims = parseJwt(token);
    if (!claims) return false;
    
    const userId = parseInt(claims.nameid, 10);
    if (isNaN(userId)) {
      console.error("Invalid user ID in token:", claims.nameid);
      return false;
    }
    
    // Store in local storage for reference
    localStorage.setItem("userId", userId.toString());
    
    // Also set a cookie
    document.cookie = `user_id=${userId}; path=/; max-age=${60*60*24*7}`;
    
    console.log("User ID verified:", userId);
    
    // Get the actual page content to verify if we show the correct user sets
    fetch("/api/UserDiagnostics/current-user")
      .then(response => response.json())
      .then(data => {
        console.log("Current user from API:", data);
      })
      .catch(err => {
        console.error("Error getting current user:", err);
      });
    
    return true;
  } catch (e) {
    console.error("Error checking user ID:", e);
    return false;
  }
};
