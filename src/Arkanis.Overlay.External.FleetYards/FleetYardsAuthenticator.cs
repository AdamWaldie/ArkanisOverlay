namespace Arkanis.Overlay.External.FleetYards;

using System.Net;
using System.Security.Claims;
using Abstractions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Overlay.Common.Errors;
using Overlay.Common.Extensions;
using Overlay.Common.Models;
using Overlay.Common.Services;
using Quartz;

public class FleetYardsAuthenticator(IServiceProvider serviceProvider) : ExternalAuthenticator<FleetYardsAuthenticator.AuthenticationTask>
{
    public override ExternalAuthenticatorInfo AuthenticatorInfo
        => FleetYardsConstants.ProviderInfo;

    public override Result ValidateCredentials(AccountCredentials? serviceCredentials)
        => serviceCredentials switch
        {
            AccountOAuth2Credentials => Result.Ok(),
            AccountOidcCredentials => Result.Ok(),
            null => Result.Ok(),
            _ => Result.Fail("Provided credentials are not valid FleetYards OAuth2 credentials."),
        };

    public override AuthenticationTask AuthenticateAsync(AccountCredentials credentials, CancellationToken cancellationToken)
        => ActivatorUtilities.CreateInstance<AuthenticationTask>(serviceProvider, credentials, cancellationToken);

    public static IJobScheduleProvider CreateRefreshJobScheduleProvider()
        => new JobScheduleProviderFactory(
            () => JobBuilder.Create<FleetYardsRefreshJob>()
                .WithIdentity($"{nameof(FleetYardsAuthenticator)}-RefreshJob")
                .WithDescription("Refreshes the FleetYards OAuth2 credentials before their expiration.")
                .SetJobData(FleetYardsRefreshJob.CreateJobData(TimeSpan.FromMinutes(30)))
                .Build(),
            () => TriggerBuilder.Create()
                .WithIdentity($"{nameof(FleetYardsAuthenticator)}-RefreshJob-Trigger")
                .WithDescription("Represents the refresh interval for FleetYards OAuth2 credentials.")
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMinutes(20))
                    .RepeatForever()
                )
                .StartNow()
                .Build()
        );

    public class AuthenticationTask(
        IFleetYardsHangarApi hangarApi,
        ILogger<AuthenticationTask> logger,
        AccountCredentials credentials,
        CancellationToken cancellationToken
    ) : AuthTaskBase(credentials, cancellationToken)
    {
        public override ExternalAuthenticatorInfo ProviderInfo
            => FleetYardsConstants.ProviderInfo;

        protected override async Task<Result<ClaimsIdentity>> RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Credentials is not AccountOAuth2Credentials oauth2Credentials)
                {
                    return Result.Fail("Provided credentials are not valid FleetYards OAuth2 credentials.");
                }

                var scopedApi = hangarApi.WithAccessToken(oauth2Credentials.AccessToken);
                return await VerifyAsync(scopedApi, cancellationToken);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning(exception, "FleetYards access token is unauthorized");
                return Result.Fail(new ExternalAccountUnauthorizedError("FleetYards access token is not valid or has expired.", exception.ToError()));
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to process FleetYards authentication");
                return exception.ToResult();
            }
        }

        private async Task<Result<ClaimsIdentity>> VerifyAsync(IFleetYardsHangarApi api, CancellationToken cancellationToken)
        {
            await api.GetHangarStatsAsync(cancellationToken);

            Identity = BuildIdentity();
            return IsAuthenticated
                ? Result.Ok(Identity)
                : Result.Fail(new ExternalAccountError("Could not establish FleetYards identity."));
        }

        private ClaimsIdentity BuildIdentity()
        {
            var claims = new List<Claim>();

            if (Credentials is AccountOAuth2Credentials oauth2)
            {
                var jwt = oauth2.ReadAccessTokenAsJwt();
                if (jwt is not null)
                {
                    if (jwt.Subject is { Length: > 0 } subject)
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, subject));
                    }

                    jwt.TryGetPayloadValue<string>("username", out var username);
                    if (string.IsNullOrEmpty(username))
                    {
                        jwt.TryGetPayloadValue<string>("preferred_username", out username);
                    }

                    if (string.IsNullOrEmpty(username))
                    {
                        jwt.TryGetPayloadValue<string>("nickname", out username);
                    }

                    if (username is { Length: > 0 })
                    {
                        claims.Add(new Claim(ClaimTypes.Name, username));
                        claims.Add(new Claim(AccountClaimTypes.DisplayName, username));
                    }
                }
            }

            return new ClaimsIdentity(claims, ProviderInfo.ServiceId, AccountClaimTypes.DisplayName, null);
        }
    }
}
