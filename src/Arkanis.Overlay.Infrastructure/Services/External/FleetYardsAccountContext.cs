namespace Arkanis.Overlay.Infrastructure.Services.External;

using Common.Abstractions;
using Microsoft.Extensions.Logging;
using Overlay.External.FleetYards;

public class FleetYardsAccountContext(
    FleetYardsAuthenticator authenticator,
    IUserPreferencesManager userPreferences,
    ILogger<FleetYardsAccountContext> logger
) : ExternalAccountContext<FleetYardsAuthenticator.AuthenticationTask>(authenticator, userPreferences, logger);
