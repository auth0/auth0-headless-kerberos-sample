using System.Net;

namespace Auth0.AdConnector.KerberosSample
{
    public class AuthorizeResult
    {
        public string Location { get; set; }

        public CookieCollection Cookies { get; set; }
    }
}