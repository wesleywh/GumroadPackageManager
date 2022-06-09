# GumroadPackageManager
This is just like the unity package manager but for gumroads `Library` page. It allows you to explore your personal collection of items that you have purchased from the gumroad store and optionally import/extract them into your unity project. 

It can download and automatically extract .unitypackage's and if it is a txt file with a google drive link, auto navigate and download the google drive contents into your project. 

Finally it caches the downloaded items on your hard drive with an easy to find path (Simply click `Open Download Folder`) in the UI.

## How To Get Your Cookie
There are some requirements in order for this to work properly. Below in the `How To Use` section it says to extract your `cookie` value. This is the `cookie` header that is sent with every gumroad request. Below will explain how you can get this cookie value. If you ever have to login to gumroad again for any reason (typing in your username and password) you will have to get this cookie value again.

 1. Login to gumroad, any page it doesn't matter
 2. On that page you logged in (Library page in image), right click on that page and click "Inspect"
<img src="https://i.imgur.com/n2nTU1W.jpg" />
 3. In the popup navigate to the "Network" tab
 <img src="https://i.imgur.com/5m5xo1y.jpg"/>
 4. Tick preserve log in case the page redirects on you
 <img src="https://i.imgur.com/Hc8kZEP.jpg"/>
 5. If there are any network events you can clear them, otherwise with this network tab still open, refresh the webpage<br/>
 6. There should be a bunch of network events happen. The first one should be the request to load the page<br/>
 7. Right click on the page load request and copy it as curl<br/>
 <img src="https://i.imgur.com/x6QSqQS.jpg" />
 8. Paste the contents into a text file<br/>
 9. Copy the "cookie" header (Blurred out in image because this is secret to you)<br/>
 <img src="https://i.imgur.com/QBjACXd.jpg" />
 10. Paste the cookie header into the `CBGames > Gumroad Package Manager > Editor > Authentication > GumroadCredentials.asset`
 <img src="https://i.imgur.com/LloyYfx.jpg" />
 11. If you did this right you can now follow the "How To Use" Section to use this. 

NOTE: You will need to refresh your cookie doing the above if you ever have to re-login to gumroad again.
 
## How To Use
 1. Navigate to `CBGames > Gumroad Package Manager > Editor > Authentication > GumroadCredentials.asset`
 2. Extract your `cookie` value from your web browser and place it into this field
 3. In your toolbar navigate to `Window > Gumroad > Package Manager`
 4. Click the refresh icon to see if it can successfully pull your personal `Library` list from gumroad.
 5. If it can you're all set! If not you probably typed something wrong in the cookie field

# LICENSE
The license file can be found in the `Editor` directory. This is under the AGPL license. Basically you can do anything you want with this (even commercially) except if you make changes to this you can't close it off and sell it. You will have to release those changes under open-source with the AGPL license as well.
