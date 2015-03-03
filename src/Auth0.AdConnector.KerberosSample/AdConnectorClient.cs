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
        private const string LoginCallbackEndpoint =
            "https://{0}/login/callback";

        private const string WsFederationEndpoint =
            "https://{0}/wsfed/{1}";

        private const string AuthorizeEndpoint =
            "https://{0}/authorize?scope=openid&response_type=code&connection={1}&sso=true&protocol=wsfed&state=&client_id={2}";

        private const string ConnectorWsFederationEndpoint =
            "{0}/wsfed?state=none&wtrealm=urn%3Aauth0%3A{1}&wa=wsignin1.0&wreply=https%3A%2F%2F{2}%2Flogin%2Fcallback";

        private readonly string _connectorUrl;
        private readonly string _tenantName;
        private readonly string _domain;
        private readonly string _clientId;
        private readonly string _connectionName;

        public AdConnectorClient(string connectorUrl, string tenantName, string domain, string clientId,
            string connectionName)
        {
            _connectorUrl = connectorUrl;
            _tenantName = tenantName;
            _domain = domain;
            _clientId = clientId;
            _connectionName = connectionName;
        }

        /// <summary>
        /// Get a SamlSecurityToken which contains all assertions.
        /// </summary>
        /// <returns></returns>
        public SamlSecurityToken GetToken()
        {
            var cookies = GetCookies();
            var wreply = GetConnectorWreply();
            var loginResponse = PostLoginCallback(wreply, cookies);
            return ReadSecurityToken(loginResponse);
        }

        public IDictionary<string, string[]> GetTokenClaims()
        {
            return GetToken().Assertion.Statements
                .OfType<SamlAttributeStatement>()
                .SelectMany(s => s.Attributes)
                .ToDictionary(a => a.Name, a => a.AttributeValues.ToArray());
        }

        /// <summary>
        /// Get the cookie from the authorize endpoint which will be used when posting the wresult back to Auth0.
        /// </summary>
        /// <returns></returns>
        private CookieCollection GetCookies()
        {
            try
            {
                var wsFedRequest =
                    WebRequest.Create(String.Format(WsFederationEndpoint, _domain, _clientId)) as HttpWebRequest;
                wsFedRequest.CookieContainer = new CookieContainer();
                wsFedRequest.AllowAutoRedirect = false;

                var cookies = new CookieCollection();
                using (var response = wsFedRequest.GetResponse() as HttpWebResponse)
                    cookies = response.Cookies;

                var authorizeRequest =
                    WebRequest.Create(String.Format(AuthorizeEndpoint, _domain, _connectionName, _clientId)) as
                        HttpWebRequest;
                authorizeRequest.CookieContainer = new CookieContainer();
                authorizeRequest.CookieContainer.Add(cookies);
                authorizeRequest.AllowAutoRedirect = false;

                using (var response = authorizeRequest.GetResponse() as HttpWebResponse)
                    return response.Cookies;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not get SSO cookie.", ex);
            }
        }

        /// <summary>
        /// Get a WS-Federation reply from the connector which can be posted to Auth0.
        /// </summary>
        /// <returns></returns>
        private string GetConnectorWreply()
        {
            try
            {
                using (var connectorClient = new WebClient())
                {
                    connectorClient.Credentials = CredentialCache.DefaultNetworkCredentials;

                    var wsFedRequest = connectorClient.DownloadString(
                        String.Format(ConnectorWsFederationEndpoint, _connectorUrl.TrimEnd('/'), _tenantName, _domain));
                    return ParseWsFederationForm(wsFedRequest);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error authenticating to the AD Connector.", ex);
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

                var request = (HttpWebRequest) HttpWebRequest.Create(String.Format(LoginCallbackEndpoint, _domain));
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);

                var requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();

                using (var response = (HttpWebResponse) request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    using (var responseReader = new StreamReader(responseStream, Encoding.Default))
                        return ParseWsFederationForm(responseReader.ReadToEnd());
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error processing login response.", ex);
            }
        }

        /// <summary>
        /// Get the security token for a response.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public SamlSecurityToken ReadSecurityToken(string response)
        {
            try
            {
                using (var sr = new StringReader(response))
                using (var reader = XmlReader.Create(sr))
                {
                    if (!reader.ReadToFollowing("saml:Assertion"))
                        throw new Exception("Assertion not found!");

                    return SecurityTokenHandlerCollection.CreateDefaultSecurityTokenHandlerCollection()
                        .ReadToken(reader.ReadSubtree()) as SamlSecurityToken;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error processing SAML assertions.", ex);
            }
        }
    }
}