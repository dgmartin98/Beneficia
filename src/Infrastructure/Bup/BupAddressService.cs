using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.Persons.Dtos;
using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Bup;

public class BupAddressService : Application.Services.IBupAddressService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly ILogger<BupAddressService> _logger;
    private readonly string _httpClientName;

    public BupAddressService(
        IHttpClientFactory factory,
        Application.Services.IBupTokenService tokenService,
        IOptions<BupApiOptions> options,
        ILogger<BupAddressService> logger,
        string httpClientName)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _httpClientName = httpClientName;
    }

    public async Task<IEnumerable<BupAddressDto>> GetAddressesByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        if (!_options.HasConfiguredClientId(BupServiceType.Addresses))
        {
            _logger?.LogInformation("Returning empty addresses for id {PersonId} because BUP is not configured.", personId);
            return Enumerable.Empty<BupAddressDto>();
        }

        try
        {
            if (!_options.HasConfiguredCredentials(BupServiceType.Addresses))
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning empty addresses for id {PersonId} because BUP is not configured.", personId);
                    return Enumerable.Empty<BupAddressDto>();
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para obtener domicilios reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(BupServiceType.Addresses, cancellationToken)
                : accessToken;

            var client = _factory.CreateClient(_httpClientName);
            _logger?.LogInformation("Llamando BUP domicilios para persona {PersonId} (catalogo {Catalog}, usuario {Username})", personId, _options.Catalog, _options.GetUsername(BupServiceType.Addresses));

            using var request = new HttpRequestMessage(HttpMethod.Get, $"addresses/datas?bupId={personId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.GetClientId(BupServiceType.Addresses));
            request.Headers.Add("UserName", _options.GetUsername(BupServiceType.Addresses));

            var response = await client.SendAsync(request, cancellationToken);
            _logger?.LogInformation("Respuesta BUP domicilios {PersonId}: {StatusCode}", personId, response.StatusCode);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (BupJsonUtils.TryGetProperty(root, out var messagesElement, "messages") && messagesElement.ValueKind == JsonValueKind.Array)
            {
                var messages = messagesElement.EnumerateArray().ToList();
                var ok = messages.Any(m => string.Equals(BupJsonUtils.GetString(m, "code"), "GSS-200-002", StringComparison.OrdinalIgnoreCase));
                if (!ok)
                {
                    throw new InvalidOperationException($"BUP addresses service returned errors: {string.Join(',', messages.Select(m => BupJsonUtils.GetString(m, \"code\") ?? BupJsonUtils.GetString(m, \"message\"))).Trim(',')}");
                }

                _logger?.LogInformation("Mensajes BUP domicilios {PersonId}: {Messages}", personId, string.Join(';', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message")).Where(m => !string.IsNullOrWhiteSpace(m))));
            }

            if (!BupJsonUtils.TryGetProperty(root, out var dataElement, "data"))
            {
                throw new InvalidOperationException("Respuesta BUP domicilios inválida: no se encontró la sección data.");
            }

            if (!BupJsonUtils.TryGetProperty(dataElement, out var addressesElement, "addresses") || addressesElement.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<BupAddressDto>();
            }

            return addressesElement
                .EnumerateArray()
                .Where(a => a.ValueKind == JsonValueKind.Object)
                .Select(MapAddress)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo domicilios BUP {PersonId}", personId);
            throw;
        }
    }

    private static BupAddressDto MapAddress(JsonElement addressElement)
    {
        var address = new BupAddressDto
        {
            AddressCode = BupJsonUtils.GetInt(addressElement, "addressCode", "domicileId", "addressId"),
            StreetName = BupJsonUtils.GetString(addressElement, "streetName", "street", "calle"),
            StreetNumber = BupJsonUtils.GetString(addressElement, "streetNumber", "number", "altura"),
            BetweenStreetOne = BupJsonUtils.GetString(addressElement, "betweenStreetOne", "betweenStreet1", "entreCalle1"),
            BetweenStreetTwo = BupJsonUtils.GetString(addressElement, "betweenStreetTwo", "betweenStreet2", "entreCalle2"),
            PostalCode = BupJsonUtils.GetString(addressElement, "postalCode", "zipCode", "codigoPostal"),
            CityName = BupJsonUtils.GetString(addressElement, "cityName", "city"),
            StateName = BupJsonUtils.GetString(addressElement, "stateName", "state"),
            CountryName = BupJsonUtils.GetString(addressElement, "countryName", "country")
        };

        if (BupJsonUtils.TryGetProperty(addressElement, out var cityElement, "city") && cityElement.ValueKind == JsonValueKind.Object)
        {
            address.CityName = address.CityName ?? BupJsonUtils.GetString(cityElement, "cityName", "name");
            address.PostalCode = address.PostalCode ?? BupJsonUtils.GetString(cityElement, "postalCode", "zipCode");

            if (BupJsonUtils.TryGetProperty(cityElement, out var stateElement, "state") && stateElement.ValueKind == JsonValueKind.Object)
            {
                address.StateName = address.StateName ?? BupJsonUtils.GetString(stateElement, "stateName", "name");
            }
        }

        if (BupJsonUtils.TryGetProperty(addressElement, out var countryElement, "country") && countryElement.ValueKind == JsonValueKind.Object)
        {
            address.CountryName = address.CountryName ?? BupJsonUtils.GetString(countryElement, "countryName", "name");
        }

        return address;
    }
}
