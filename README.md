# Auth0 Headless Kerberos authentication for the AD Connector

Sample project showing how to do headless Kerberos authentication with Auth0 (for Console Applications, Windows Services, ...)

## Key features

Use "Integrated Windows Authentication" (Kerberos) from your Console applications, Windows Services, ... with Auth0. This allows you to have Kerberos support for headless application that run in the context of a Windows User (on a Domain Joined machine).

## Install

You'll typically want to use this to end up with an `id_token` which you can then use to call APIs secured with Auth0. By default the id_token is not part for the SAML response. If you need the `id_token` you'll need to add a rule to add it to the outgoing claims: [Generate a JSON Web Token](https://github.com/auth0/rules/blob/master/rules/jwt.md)

## Usage

First you'll need to add the required settings in the App.config/Web.config:

```xml
<add key="auth0:Domain" value="fabrikam.auth0.com" />
<add key="auth0:TenantName" value="fabrikam" />
<add key="auth0:ADConnectorUrl" value="http://fabrikam-dc:57303" />
<add key="auth0:ClientID" value="yNqQMENaYIONxAaQmrct341tZ9joEjTi" />
<add key="auth0:ConnectionName" value="FabrikamAD" />
```

And now you can use the `AdConnectorClient` for headless Kerberos authentication and to get the **current user**'s claims:

```csharp
var client = new AdConnectorClient(
    ConfigurationManager.AppSettings["auth0:ADConnectorUrl"],
    ConfigurationManager.AppSettings["auth0:TenantName"],
    ConfigurationManager.AppSettings["auth0:Domain"],
    ConfigurationManager.AppSettings["auth0:ClientID"],
    ConfigurationManager.AppSettings["auth0:ConnectionName"]);

var claims = client.GetTokenClaims();
foreach (var claim in claims)
{
    Console.WriteLine(" > {0}\r\n   {1}\r\n", claim.Key, String.Join(", ", claim.Value));
}
```

Example:

![](https://cdn.auth0.com/docs/img/ad-kerberos-headless.png)

## What is Auth0?

Auth0 helps you to:

* Add authentication with [multiple authentication sources](https://docs.auth0.com/identityproviders), either social like **Google, Facebook, Microsoft Account, LinkedIn, GitHub, Twitter, Box, Salesforce, amont others**, or enterprise identity systems like **Windows Azure AD, Google Apps, Active Directory, ADFS or any SAML Identity Provider**.
* Add authentication through more traditional **[username/password databases](https://docs.auth0.com/mysql-connection-tutorial)**.
* Add support for **[linking different user accounts](https://docs.auth0.com/link-accounts)** with the same user.
* Support for generating signed [Json Web Tokens](https://docs.auth0.com/jwt) to call your APIs and **flow the user identity** securely.
* Analytics of how, when and where users are logging in.
* Pull data from other sources and add it to the user profile, through [JavaScript rules](https://docs.auth0.com/rules).

## Create a free Auth0 Account

1. Go to [Auth0](https://auth0.com) and click Sign Up.
2. Use Google, GitHub or Microsoft Account to login.

## Issue Reporting

If you have found a bug or if you have a feature request, please report them at this repository issues section. Please do not report security vulnerabilities on the public GitHub issue tracker. The [Responsible Disclosure Program](https://auth0.com/whitehat) details the procedure for disclosing security issues.

## Author

[Auth0](auth0.com)

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
