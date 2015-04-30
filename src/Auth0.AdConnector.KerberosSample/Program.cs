using System;
using System.Configuration;

namespace Auth0.AdConnector.KerberosSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new AdConnectorClient(
                ConfigurationManager.AppSettings["auth0:Domain"],
                ConfigurationManager.AppSettings["auth0:ClientID"],
                ConfigurationManager.AppSettings["auth0:ConnectionName"],
                "openid email nickname");

            var result = client.Authenticate();
            foreach (var item in result)
            {
                Console.WriteLine(" > {0}: {1}", item.Key, String.Join(", ", item.Value));
            }

            Console.ReadLine();
        }
    }
}