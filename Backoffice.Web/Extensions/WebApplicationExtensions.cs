namespace Backoffice.Web.Extensions
{
    public static class WebApplicationExtensions
    {
        public const string DetailedErrorsKey = "DetailedErrors";

        // No argocd values file sets ASPNETCORE_ENVIRONMENT, so DEV, STG and PRD all run as Production.
        // Detail is shown locally (Development) or where the DetailedErrors key is on: appsettings.Development.json
        // locally, a DetailedErrors env var in DEV. The Blazor circuit option binds that same key itself (BO-132).
        public static bool ShowDetailedErrors(IConfiguration configuration, IHostEnvironment environment) =>
            environment.IsDevelopment() || configuration[DetailedErrorsKey]?.ToLowerInvariant() is "true" or "1";

        // The developer exception page sends stack traces, file paths, headers and cookies to the browser.
        // Everywhere else the exception is logged by the handler middleware and the user gets the generic /Error page.
        public static WebApplication UseErrorPages(this WebApplication app)
        {
            if (ShowDetailedErrors(app.Configuration, app.Environment))
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            return app;
        }

        // Second layer behind encoding (BO-134). Only directives that cannot break the current pages: no plugins,
        // no <base> hijack, no framing by other sites. A script-src policy needs the inline scripts in _Host/_Layout
        // moved to files first (docs/08_TechnicalDebt.md).
        public const string ContentSecurityPolicy = "object-src 'none'; base-uri 'self'; frame-ancestors 'self'";

        public static WebApplication UseSecurityHeaders(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                context.Response.Headers["Content-Security-Policy"] = ContentSecurityPolicy;
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                await next();
            });

            return app;
        }
    }
}
