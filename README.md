# flashcard app
fullstack project. C# .NET, MVC, SSR. 
User Accounts &amp; Profiles, Flashcard Sets, Favourites System, Friends System, Feed ("Flow") Page.

<hr>

an app that people could create, put into their favourites, and share flashcard sets (publicly -to everyone-, with specific friends, or all friends)

profile page (password change, some profile edit parts)
* my fav sets
* my sets

my friends page (including sending friend request and seeing requests)
* my friends (public or open to me) sets

something like "flow"/feed in instagram/x or other social platforms
* every set would have tags, when i create or fav sets, that kind of sets would appear to me more.

### TODO
* All the text seen by users will be Turkish. English may be added as a choice.
* Token url'de parametre olarak gözükmemeli.
* Controller security should be checked again. some does not have friends check (for sets) or owner check.
    * these should be checked using functions to be avoid repeating the same things.
* 404, 403 etc. pages
* Admin panel to manage tags and users
* activity_logs to log user actions
* audit_logs to log admin actions
* REST API for future mobile app

### TODO (a bit extra)
* Export set to PDF or CSV
* levels of admins (0, 1, 2) -like mod, admin, root etc.-
* prevent abuse on “most viewed sets”
    * Add an IP hash
    * Add a UNIQUE(user_id, set_id, DATE(viewed_at)) to limit one view per day per user per set
* Notifications (new friend request, someone liked your set, etc.)
* Flashcard "Study Mode" with one-by-one flip animation
* Spaced repetition tracking (basic implementation)
