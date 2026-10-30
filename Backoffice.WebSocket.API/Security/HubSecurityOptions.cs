namespace Backoffice.WebSocket.API.Security
{
    /// <summary>
    /// Who may connect to /bohub and who may call the Publish* methods (BO-131).
    /// Enforce=false only logs callers that would be refused, so the clients can be deployed with
    /// their credentials first; turn it on once they all send one.
    /// </summary>
    public class HubSecurityOptions
    {
        public bool Enforce { get; init; }
        public string PublisherApiKey { get; init; }
        public string SubscriberApiKey { get; init; }
        public string JwtKey { get; init; }
        public string JwtIssuer { get; init; }

        public static HubSecurityOptions FromConfiguration(IConfiguration configuration)
        {
            var options = new HubSecurityOptions
            {
                Enforce = configuration.GetValue<bool>("Security:Enforce"),
                PublisherApiKey = configuration["Security:PublisherApiKey"],
                SubscriberApiKey = configuration["Security:SubscriberApiKey"],
                JwtKey = configuration["API:AuthenticationAPI:JWT:Key"],
                JwtIssuer = configuration["API:AuthenticationAPI:JWT:Issuer"]
            };

            if (options.Enforce)
            {
                // Fail at startup rather than refusing every caller with a half-configured hub.
                if (string.IsNullOrWhiteSpace(options.PublisherApiKey) || string.IsNullOrWhiteSpace(options.SubscriberApiKey))
                    throw new InvalidOperationException("Security:Enforce is on but Security:PublisherApiKey or Security:SubscriberApiKey is missing.");
                if (options.PublisherApiKey == options.SubscriberApiKey)
                    throw new InvalidOperationException("Security:PublisherApiKey and Security:SubscriberApiKey must be different.");
                if (string.IsNullOrWhiteSpace(options.JwtKey) || string.IsNullOrWhiteSpace(options.JwtIssuer))
                    throw new InvalidOperationException("Security:Enforce is on but API:AuthenticationAPI:JWT:Key or JWT:Issuer is missing.");
            }

            return options;
        }
    }

    public static class HubRoles
    {
        public const string Publisher = "Publisher";
        public const string Subscriber = "Subscriber";
    }

    public static class HubPolicies
    {
        public const string Connect = "HubConnect";
        public const string Publish = "HubPublish";
    }

    public static class HubClaims
    {
        public const string UserId = "UserId";
    }
}
