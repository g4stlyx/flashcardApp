# flashcard app
fullstack project. C# .NET, MVC, SSR. 

* online at http://flashcard-app.g4stly.tr/ (deployed for now, i will remove it from heroku after presentation until i have a physical server)

* an app that people could create, put into their favourites, and share flashcard sets (publicly -to everyone-, or with friends)

* User Accounts &amp; Profiles, Flashcard Sets, Favourites System, Friends System, Feed ("Flow") Page.

### TODO (after the presentation)
* all console logs will be cleared.
    * \+ the auth tests etc. 
* optimization + DRY
* profile page (password change, some profile edit parts)
* 404, 403, 401, 500 etc. pages
* liking sets functionality, my fav sets page.
* Controller security should be checked again. some does not have friends check (for sets) or owner check.
    * these should be checked using functions to be avoid repeating the same things.
* Token url'de parametre olarak gözükmemeli. (in FlascardsView/MySets)
* something like "flow"/feed in instagram/x or other social platforms
    * every set would have tags, when i create or fav sets, that kind of sets would appear to me more.
    * herkese açık setler zaten var, bunun öneri/sıralama mantığı değişir.
* english language option
* dark/light theme option
* activity_logs to log user actions
* audit_logs to log admin actions
* REST API for future mobile app
* Export set to PDF or CSV
* levels of admins (0, 1, 2) -like mod, admin, root etc.-
* prevent abuse on “most viewed sets”
    * Add an IP hash
    * Add a UNIQUE(user_id, set_id, DATE(viewed_at)) to limit one view per day per user per set
* Notifications (new friend request, someone liked your set, etc.)
* Flashcard "Study Mode" with one-by-one flip animation
* Spaced repetition tracking (basic implementation)
