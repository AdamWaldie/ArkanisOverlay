namespace Arkanis.Overlay.External.FleetYards;

using Duende.IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Overlay.Common.Abstractions;
using Overlay.Common.Models;
using Overlay.Common.Services;
using Quartz;

public partial class FleetYardsRefreshJob(
    FleetYardsAuthenticator authenticator,
    IUserPreferencesManager preferencesManager,
    IHttpClientFactory httpClientFactory,
    ILogger<FleetYardsRefreshJob> logger
) : IJob
{
    private const string RefreshThresholdKey = nameof(RefreshThresholdKey);

    public static JobDataMap CreateJobData(TimeSpan? refreshThreshold = null)
    {
        var dataMap = new JobDataMap();
        if (refreshThreshold.HasValue)
        {
            dataMap[RefreshThresholdKey] = refreshThreshold.Value;
        }

        return dataMap;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var credentials = preferencesManager.CurrentPreferences.GetCredentialsOrDefaultFor(ExternalService.FleetYards);
        if (credentials is not AccountOAuth2Credentials { RefreshToken.Length: > 0 } oauth2Credentials)
        {
            LogNoValidCredentialsFound(logger);
            return;
        }

        if (!context.MergedJobDataMap.TryGetTimeSpan(RefreshThresholdKey, out var refreshThreshold))
        {
            refreshThreshold = TimeSpan.FromMinutes(30);
        }

        var minimumValidDate = DateTimeOffset.UtcNow.Add(refreshThreshold);
        if (oauth2Credentials.AccessTokenExpiresAt > minimumValidDate
            || (oauth2Credentials.ReadAccessTokenAsJwt() is { } jwt && jwt.ValidTo > minimumValidDate))
        {
            LogNotExpiringSoon(logger);
            return;
        }

        var httpClient = httpClientFactory.CreateClient(nameof(FleetYardsRefreshJob));
        var tokenResponse = await httpClient.RequestRefreshTokenAsync(
            new RefreshTokenRequest
            {
                Address = FleetYardsConstants.OAuthTokenUrl,
                RefreshToken = oauth2Credentials.RefreshToken,
            }
        );

        if (tokenResponse.IsError)
        {
            LogRefreshFailed(logger, tokenResponse.Error, tokenResponse.ErrorDescription);
            authenticator.RequestRefresh();
            return;
        }

        var updatedCredentials = oauth2Credentials with
        {
            AccessToken = tokenResponse.AccessToken ?? oauth2Credentials.AccessToken,
            AccessTokenExpiresAt = tokenResponse.ExpiresIn > 0
                ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                : oauth2Credentials.AccessTokenExpiresAt,
            RefreshToken = tokenResponse.RefreshToken ?? oauth2Credentials.RefreshToken,
        };

        var updatedPreferences = preferencesManager.CurrentPreferences.SetCredentials(updatedCredentials);
        await preferencesManager.SaveAndApplyUserPreferencesAsync(updatedPreferences);
        LogRefreshSucceeded(logger);
    }

    [LoggerMessage(LogLevel.Debug, "No FleetYards OAuth2 credentials with refresh token found, skipping refresh")]
    static partial void LogNoValidCredentialsFound(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "FleetYards access token is not expiring soon, skipping refresh")]
    static partial void LogNotExpiringSoon(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Could not refresh FleetYards credentials: {error} - {errorDescription}")]
    static partial void LogRefreshFailed(ILogger logger, string? error, string? errorDescription);

    [LoggerMessage(LogLevel.Debug, "FleetYards credentials refreshed successfully")]
    static partial void LogRefreshSucceeded(ILogger logger);
}
