# GumroadPackageManager
This is just like the unity package manager but for gumroads `Library` page. It allows you to explore your personal collection of items that you have purchased from the gumroad store and optionally import/extract them into your unity project. 

It can download and automatically extract .unitypackage's and if it is a txt file with a google drive link, auto navigate and download the google drive contents into your project. 

Finally it caches the downloaded items on your hard drive with an easy to find path (Simply click `Open Download Folder`) in the UI.

## How To Use
 1. Navigate to `CBGames > Gumroad Package Manager > Editor > Authentication > GumroadCredentials.asset`
 2. Extract your `cookie` value from your web browser and place it into this field
 3. In your toolbar navigate to `Window > Gumroad > Package Manager`
 4. Click the refresh icon to see if it can successfully pull your personal `Library` list from gumroad.
 5. If it can you're all set! If not you probably typed something wrong in the cookie field
