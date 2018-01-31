using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

using OwinWebApiTest.Models;

namespace OwinWebApiTest.Providers
{
    /**
     * CAS Single Sign On Authorization Provider.
     *
     * Authenticates user against CAS server login and grants authorization;
     * provides authenticated user with access_token and AccessControl dataset.
     */
    public class CasAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        private static string casValidationUrl;
        private static string serviceUser;

        public CasAuthorizationServerProvider()
        {
            casValidationUrl = ConfigurationManager.AppSettings["CasHost"]
                + ConfigurationManager.AppSettings["CasValidationPath"];
            serviceUser = ConfigurationManager.AppSettings["ServiceUser"];
        }

        /**
         * Client authentication
         * 
         * Superseded by CAS authentication but is required, so just validate
         */
        public override async Task ValidateClientAuthentication(
            OAuthValidateClientAuthenticationContext context)
        {
            await Task.FromResult(context.Validated());
        }

        /**
         * Performs CAS ticket validation & grants authorization
         *
         * Expected params (POST method):
         * "ticket" (provided to the client by the CAS server on a successful login)
         * "service" (application url - against which the login attempt was performed)
         * "grant_type=password"
         *
         * See TokenEndpointPath in Startup.cs for the autorization entry point URL
         *
         * If successful, grants authorization and returns client a response with
         * a valid "access_token", "username" and (app-specific) "AccessControl" dataset
         */
        public override async Task GrantResourceOwnerCredentials(
            OAuthGrantResourceOwnerCredentialsContext context)
        {
            dynamic args = await context.Request.ReadFormAsync();

            if (string.IsNullOrEmpty(args["ticket"]) || string.IsNullOrEmpty(args["service"])) {
                context.Rejected();
                context.SetError("invalid_grant", "No CAS ticket or service URL sent");
                return;
            }

            var res = await ValidateCasTicket(args["ticket"], args["service"]);

            if (res.success == null && !string.IsNullOrEmpty(serviceUser)) {
                res.success = new CasServiceValidationSuccess { user = serviceUser };
            }

            if (res.success == null) {
                context.Rejected();
                context.SetError("invalid_grant", "CAS validation failed: " + (res.failure != null
                    ? res.failure.description : "No response received from the CAS server"));
                return;
            }

            // once the CAS auth is done, gather additional data about the user (app-specific)
            //var acda = new AccessControlDA();
            //var ac = acda.GetAccessControl(res.success.user);
            var ac = new { userId = res.success.user, canRead = true, canSave = true };

            if (ac == null) {
                context.Rejected();
                context.SetError("invalid_grant", $"User '{res.success.user}' not found");
                return;
            }

            ClaimsIdentity identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, res.success.user));
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));

            // To add app-specific data to the access token response use AuthenticationProperties
            // as below, for plain access token response identity will be encoded into it this way:
            //context.Validated(identity);

            var props = new AuthenticationProperties(new Dictionary<string, string> {
                { "username", res.success.user },
                { "AccessControl", JsonConvert.SerializeObject(ac) },
            });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);
        }

        /**
         * Necessary to add the AuthenticationProperties to the AuthenticationTicket
         */
        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }

        /**
         * Validates CAS ticket received from the frontend against the CAS server
         */
        private async Task<CasServiceValidationResponse> ValidateCasTicket(string ticket, string service)
        {
            var requestUri = WebUtilities.AddQueryString(casValidationUrl, new Dictionary<string, string>() {
                { "service", service },
                { "ticket", ticket },
                { "format", "JSON" },
            });

            using (HttpClient client = new HttpClient())
            {
                return await GetCasServiceValidationAsync(client, requestUri);
            }
        }

        /**
         * Sends the CAS ticket validation request and gets relevant data from a response
         */
        public async Task<CasServiceValidationResponse> GetCasServiceValidationAsync(
            HttpClient client, string requestUri)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    dynamic resp = await response.Content.ReadAsAsync<JObject>();
                    var success = resp.SelectToken("serviceResponse.authenticationSuccess");
                    var failure = resp.SelectToken("serviceResponse.authenticationFailure");

                    return new CasServiceValidationResponse() {
                        success = success != null ? success.ToObject<CasServiceValidationSuccess>() : null,
                        failure = failure != null ? failure.ToObject<CasServiceValidationFailure>() : null,
                    };
                }
            }
        }
    }
}