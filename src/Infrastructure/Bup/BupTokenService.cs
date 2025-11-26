using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using Application.Services;
using Infrastructure.Bup.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Bup;

public class BupTokenService : Application.Services.IBupTokenService
{
    private readonly IHttpClientFactory _factory;
    private readonly BupApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BupTokenService> _logger;
    private readonly Dictionary<BupServiceType, TokenCache> _tokenCache;
    private readonly string _peopleClientName;
    private readonly string _phonesClientName;
    private readonly string _emailsClientName;

    private sealed class TokenCache
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public string? AccessToken { get; set; }
        public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.MinValue;
    }

    public BupTokenService(IHttpClientFactory factory, IOptions<BupApiOptions> options, ILogger<BupTokenService> logger, string peopleClientName, string phonesClientName, string emailsClientName)
    {
        _factory = factory;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _logger = logger;
        _peopleClientName = peopleClientName;
        _phonesClientName = phonesClientName;
        _emailsClientName = emailsClientName;
        _tokenCache = new Dictionary<BupServiceType, TokenCache>
        {
            [BupServiceType.Person] = new TokenCache(),
            [BupServiceType.Phones] = new TokenCache(),
            [BupServiceType.Emails] = new TokenCache()
        };
    }

    public async Task<string> GetTokenAsync(BupServiceType serviceType, CancellationToken cancellationToken)
    {
        var cache = _tokenCache[serviceType];
        if (!string.IsNullOrEmpty(cache.AccessToken) && DateTimeOffset.UtcNow < cache.ExpiresAt)
        {
            _logger?.LogDebug("Usando token BUP cacheado para {ServiceType}, válido hasta {ExpiresAt}", serviceType, cache.ExpiresAt);
            return cache.AccessToken;
        }

        await cache.Semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(cache.AccessToken) && DateTimeOffset.UtcNow < cache.ExpiresAt)
            {
                _logger?.LogDebug("Usando token BUP cacheado para {ServiceType} luego de esperar, válido hasta {ExpiresAt}", serviceType, cache.ExpiresAt);
                return cache.AccessToken;
            }

            if (!_options.HasConfiguredCredentials(serviceType))
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogWarning("BUP credentials not configured for {ServiceType}. Using local mock token for development.", serviceType);
                    cache.AccessToken = "local-dev-token";
                    cache.ExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
                    return cache.AccessToken;
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para solicitar tokens reales.");
            }

            var client = _factory.CreateClient(GetClientName(serviceType));

            var request = new HttpRequestMessage(HttpMethod.Post, "security/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "apigss"
                })
            };

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.GetClientId(serviceType)}:{_options.GetClientSecret(serviceType)}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            try
            {
                _logger?.LogInformation("Solicitando nuevo token BUP para {ServiceType} (catalogo {Catalog})", serviceType, _options.Catalog);
                var response = await client.SendAsync(request, cancellationToken);
                _logger?.LogInformation("Respuesta de token BUP {ServiceType}: {StatusCode}", serviceType, response.StatusCode);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadFromJsonAsync<BupTokenRaw>(_jsonOptions, cancellationToken);
                if (raw?.access_token == null)
                    throw new InvalidOperationException("No se obtuvo access_token desde BUP");

                cache.AccessToken = raw.access_token;
                var expiresIn = raw.expires_in ?? 300;
                cache.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);

                _logger?.LogInformation("Token BUP obtenido para {ServiceType}, expira a las {ExpiresAt}", serviceType, cache.ExpiresAt);

                return cache.AccessToken;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogWarning(ex, "Error obteniendo token desde BUP para {ServiceType}.", serviceType);
                throw;
            }
        }
        finally
        {
            cache.Semaphore.Release();
        }
    }

    private string GetClientName(BupServiceType serviceType) => serviceType switch
    {
        BupServiceType.Person => _peopleClientName,
        BupServiceType.Phones => _phonesClientName,
        BupServiceType.Emails => _emailsClientName,
        _ => _phonesClientName
    };
}
