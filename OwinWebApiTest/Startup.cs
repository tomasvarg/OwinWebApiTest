using System;
using System.Configuration;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.OAuth;
using System.Web.Http;
using System.Net;

using OwinWebApiTest.Providers;

[assembly: OwinStartup(typeof(OwinWebApiTest.Startup))]

namespace OwinWebApiTest
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);

            double tokenLifetime;
            double.TryParse(ConfigurationManager.AppSettings["AccessTokenLifetimeHours"], out tokenLifetime);

            // token configuration
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/api/auth/validate"),
                AccessTokenExpireTimeSpan = TimeSpan.FromHours(tokenLifetime != 0 ? tokenLifetime : 10),
                //Provider = new SimpleAuthorizationServerProvider()
                Provider = new CasAuthorizationServerProvider()
            });

            // token consumption
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            HttpConfiguration config = new HttpConfiguration();
            app.UseWebApi(WebApiConfig.Register(config));

            // allow self-signed certificates
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }
    }
}
