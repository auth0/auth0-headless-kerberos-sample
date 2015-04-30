using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using HtmlAgilityPack;

namespace Auth0.AdConnector.KerberosSample
{
    public class AdConnectorClient
    {
        private const string DefaultCallbackUrl =
            "http://headless.local";

        private const string LoginCallbackEndpoint =
            "https://{0}/login/callback";

        private const string AuthorizeEndpoint =
            "https://{0}/authorize?scope={4}&response_type=token&connection={1}&sso=true&state=&client_id={2}&redirect_uri={3}";

        private readonly string _scope;
        private readonly string _domain;
        private readonly string _clientId;
        private readonly string _connectionName;
        private readonly string _callbackUrl;

        public AdConnectorClient(string domain, string clientId, string connectionName, string scope = "openid", string callbackUrl = null)
        {
            _scope = scope;
            _domain = domain;
            _clientId = clientId;
            _connectionName = connectionName;
            _callbackUrl = callbackUrl ?? DefaultCallbackUrl;
        }

        public IDictionary<string, string> Authenticate()
        {
            var authorizeResult = CallAuthorize();
            var wreply = GetConnectorWreply(authorizeResult.Location);
            var loginResponse = PostLoginCallback(wreply, authorizeResult.Cookies);
            return loginResponse.Split('#')[1].Split('&')
                .ToDictionary(c => c.Split('=')[0], c => Uri.UnescapeDataString(c.Split('=')[1]));
        }

        /// <summary>
        /// Get the cookie from the authorize endpoint which will be used when posting the wresult back to Auth0.
        /// </summary>
        /// <returns></returns>
        private AuthorizeResult CallAuthorize()
        {
            var authorizeRequest = WebRequest.Create(String.Format(AuthorizeEndpoint, _domain, _connectionName, _clientId, WebUtility.UrlEncode(_callbackUrl), _scope)) as HttpWebRequest;
            authorizeRequest.CookieContainer = new CookieContainer();
            authorizeRequest.AllowAutoRedirect = false;

            using (var response = authorizeRequest.GetResponse() as HttpWebResponse)
            {
                return new AuthorizeResult
                {
                    Cookies = response.Cookies,
                    Location = response.Headers["Location"]
                };
            }
        }

        /// <summary>
        /// Get a WS-Federation reply from the connector which can be posted to Auth0.
        /// </summary>
        /// <returns></returns>
        private string GetConnectorWreply(string location)
        {
            using (var connectorClient = new WebClient())
            {
                connectorClient.Credentials = CredentialCache.DefaultNetworkCredentials;

                var wsFedRequest = connectorClient.DownloadString(location);
                return ParseWsFederationForm(wsFedRequest);
            }
        }

        /// <summary>
        /// Get the wresult from a WS Federation POST form.
        /// </summary>
        /// <param name="htmlForm"></param>
        /// <returns></returns>
        private string ParseWsFederationForm(string htmlForm)
        {
            try
            {

                var wsFedRequestPage = new HtmlDocument();
                wsFedRequestPage.LoadHtml(htmlForm);

                var wresult =
                    wsFedRequestPage.DocumentNode.SelectSingleNode("//input[@name='wresult']")
                        .GetAttributeValue("value", null);
                return WebUtility.HtmlDecode(wresult);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to find a value for 'wresult'.", ex);
            }
        }

        /// <summary>
        /// Call the /login/callback endpoint and process the response.
        /// </summary>
        /// <param name="wresult"></param>
        /// <returns></returns>
        private string PostLoginCallback(string wresult, CookieCollection cookies)
        {
            try
            {
                byte[] data =
                    Encoding.ASCII.GetBytes("wa=wsignin1.0&wctx=undefined&wresult=" + WebUtility.UrlEncode(wresult));

                var request = (HttpWebRequest)HttpWebRequest.Create(String.Format(LoginCallbackEndpoint, _domain));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
                request.AllowAutoRedirect = false;

                var requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                // Get the location.s
                using (var response = (HttpWebResponse)request.GetResponse())
                    return response.Headers["Location"];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error processing login response.", ex);
            }
        }
    }
}