using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.Security;
using Microsoft.Owin.Infrastructure;
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

        public override async Task ValidateClientAuthentication(
            OAuthValidateClientAuthenticationContext context)
        {
            // required but as we're not using client auth just validate & move on...
            await Task.FromResult(context.Validated());
        }

        public override async Task GrantResourceOwnerCredentials(
            OAuthGrantResourceOwnerCredentialsContext context)
        {
            dynamic args = await context.Request.ReadFormAsync();

            if (string.IsNullOrEmpty(args["ticket"]) || string.IsNullOrEmpty(args["service"])) {
                context.SetError("invalid_grant", "No CAS ticket or service URL sent.");
                context.Rejected();
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

            //var acda = new AccessControlDA();
            //var ac = acda.GetAccessControl(res.success.user);
            var ac = new { userId = args["username"], saveAllowed = true, saveAllUnits = true };

            ClaimsIdentity identity = new ClaimsIdentity(context.Options.AuthenticationType);
            identity.AddClaim(new Claim(ClaimTypes.Name, res.success.user));
            identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
            //identity.AddClaim(new Claim("user_name", context.UserName));
            //identity.AddClaim(new Claim("sub", context.UserName));

            // Identity info will be encoded into an Access ticket as a result of this call:
            //context.Validated(identity);

            var props = new AuthenticationProperties(new Dictionary<string, string> {
                { "username", res.success.user },
                { "AccessControl", JsonConvert.SerializeObject(ac) },
            });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);
        }

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