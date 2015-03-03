using System;
using System.Configuration;

namespace Auth0.AdConnector.KerberosSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new AdConnectorClient(
                ConfigurationManager.AppSettings["auth0:ADConnectorUrl"],
                ConfigurationManager.AppSettings["auth0:TenantName"],
                ConfigurationManager.AppSettings["auth0:Domain"],
                ConfigurationManager.AppSettings["auth0:ClientID"],
                ConfigurationManager.AppSettings["auth0:ConnectionName"]);

            var claims = client.GetTokenClaims();
            foreach (var claim in claims)
            {
                Console.WriteLine(" > {0}: {1}", claim.Key, String.Join(", ", claim.Value));
            }

            Console.ReadLine();
        }
    }
}