using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Owin;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
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

            string webDir = ConfigurationManager.AppSettings["WebDirectory"];
            if (string.IsNullOrEmpty(webDir)) webDir = "Web";
            app.UseFileServer(GetFileServerOptions(PathString.Empty, webDir));

            string docDir = ConfigurationManager.AppSettings["DocDirectory"];
            if (string.IsNullOrEmpty(docDir)) docDir = "Doc";
            app.UseFileServer(GetFileServerOptions(new PathString("/doc"), docDir));
        }

        private FileServerOptions GetFileServerOptions(PathString pathString, string dir)
        {
            string appRoot = AppDomain.CurrentDomain.BaseDirectory;
            var fileSystem = new PhysicalFileSystem(Path.Combine(appRoot, dir));

            var options = new FileServerOptions
            {
                RequestPath = pathString,
                EnableDefaultFiles = true,
                FileSystem = fileSystem
            };
            options.StaticFileOptions.FileSystem = fileSystem;
            options.StaticFileOptions.ServeUnknownFileTypes = false;

            return options;
        }
    }
}
