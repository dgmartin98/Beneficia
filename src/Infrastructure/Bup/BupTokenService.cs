using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Infrastructure.Bup.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Application.Services;

namespace Infrastructure.Bup;

public class BupTokenService : Application.Services.IBupTokenService
{
    private readonly IHttpClientFactory _factory;
    private readonly BupApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<BupTokenService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public BupTokenService(IHttpClientFactory factory, IOptions<BupApiOptions> options, ILogger<BupTokenService> logger)
    {
        _factory = factory;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _expiresAt)
            return _accessToken;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _expiresAt)
                return _accessToken;

            // If credentials are not configured, either return a mock (when explicitly enabled) or fail fast
            if (!_options.HasConfiguredCredentials())
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogWarning("BUP credentials not configured (ClientId/ClientSecret). Using local mock token for development.");
                    _accessToken = "local-dev-token";
                    _expiresAt = DateTimeOffset.UtcNow.AddHours(1);
                    return _accessToken;
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para solicitar tokens reales.");
            }

            var client = _factory.CreateClient("BupApi");

            var request = new HttpRequestMessage(HttpMethod.Post, "security/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["scope"] = "apigss"
                })
            };

            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            try
            {
                var response = await client.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadFromJsonAsync<BupTokenRaw>(_jsonOptions, cancellationToken);
                if (raw?.access_token == null)
                    throw new InvalidOperationException("No se obtuvo access_token desde BUP");

                _accessToken = raw.access_token;
                var expiresIn = raw.expires_in ?? 300;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 30);

                return _accessToken;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogWarning(ex, "Error obteniendo token desde BUP.");
                // If we can't reach BUP but credentials are present, rethrow so caller knows.
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
