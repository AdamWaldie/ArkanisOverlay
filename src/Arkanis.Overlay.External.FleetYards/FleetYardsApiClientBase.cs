namespace Arkanis.Overlay.External.FleetYards;

using System.ComponentModel.Design;
using System.Net.Http.Headers;
using Abstractions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal abstract class FleetYardsApiClientBase : IFleetYardsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<FleetYardsApiOptions> _options;

    [UsedImplicitly]
    protected FleetYardsApiClientBase()
    {
    }

    protected FleetYardsApiClientBase(IHttpClientFactory httpClientFactory, IOptionsMonitor<FleetYardsApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public FleetYardsApiOptions CurrentOptions
        => _options.CurrentValue;

    public FleetYardsApiOptions? OverrideOptions { get; set; }

    protected string BaseUrl
        => (OverrideOptions ?? _options.CurrentValue).BaseUrl.TrimEnd('/');

    public ValueTask<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient(GetType().Name);

        var options = OverrideOptions ?? _options.CurrentValue;
        httpClient.Timeout = options.Timeout;

        if (options.AccessToken is { Length: > 0 } token)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return ValueTask.FromResult(httpClient);
    }

    public TClient CloneAs<TClient>() where TClient : class, IFleetYardsApiClient
        => ActivatorUtilities.CreateInstance(new ServiceContainer(), GetType(), _httpClientFactory, _options) as TClient
           ?? throw new InvalidOperationException($"Could not clone {GetType()} as {typeof(TClient)}.");
}
