using UnityEngine;

namespace Gumroad.API
{
    [CreateAssetMenu(fileName = "GumroadCreds", menuName = "Gumroad/Package Manager/Create API Credentials", order = 1)]
    public class GumroadCredentials : ScriptableObject
    {
        [Header("API")]
        [Tooltip("The token you get when you generate an application to access your publisher information in gumroads website.")]
        public string token = null;
        [Tooltip("The host http header needed to authenticate with the api.")]
        public string host_api = "api.gumroad.com";
        [Space(10)]
        [Header("WEB")]
        [Tooltip("The cookie http header needed to authenticate with the web")]
        public string cookie = null;
        [Tooltip("The host http header needed to authenticate with the web.")]
        public string host_web = "app.gumroad.com";
    }
}