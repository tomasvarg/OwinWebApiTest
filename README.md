# Owin WebApi Auth Test

ASP.NET OWIN WebAPI setup with CAS authorization and static file serving.

See [`Startup.cs`](./OwinWebApiTest/Startup.cs) and
[`Providers/CasAuthorizationServerProvider.cs`](./OwinWebApiTest/Providers/CasAuthorizationServerProvider.cs)
 files for the most relevant stuff.

## Configuration

Options configurable in project's `Web.config`:

- `appSettings/CasHost`: URL of the CAS authentication server (required)
- `appSettings/CasValidationPath`: CAS token validation URL path
  (required; for case another protocol version desired)
- `appSettings/AccessTokenLifetimeHours`: login expiration in hours (optional, default = 10)
- `appSettings/WebDirectory`: directory from which the client app is served (optional, default = 'Web')
- `appSettings/DocDirectory`: directory from which the documentation is served (optional, default = 'Doc')
- `appSettings/ServiceUser`: bypass ticket authentication, authorize this user (optional)

A [CAS](https://apereo.github.io/cas) server like
[cas-gradle-overlay-template](https://github.com/apereo/cas-gradle-overlay-template)
expected on `appSettings/CasHost` URL.