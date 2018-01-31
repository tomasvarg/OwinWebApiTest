using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace OwinWebApiTest
{
    /**
     * Configuration of the web service options.
     */
    public static class WebApiConfig
    {
        public static HttpConfiguration Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "DetailApi",
                routeTemplate: "api/{controller}/{resource}/{id}"
            );

            // require authorization for all the entrypoints
            // can be overriden by [AllowAnonymous] attribute (at the controller/service level)
            config.Filters.Add(new AuthorizeAttribute());

            return config;
        }
    }
}
