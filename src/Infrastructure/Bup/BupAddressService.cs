using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
                var messageDetails = messages
                    .Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message"))
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToList();

                var ok = messageDetails.Any(code =>
                    string.Equals(code, "GSS-200-002", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(code, "GS-200-002", StringComparison.OrdinalIgnoreCase));

                if (!ok)
                {
                    throw new InvalidOperationException($"BUP addresses service returned errors: {string.Join(',', messageDetails)}");
                }

                _logger?.LogInformation("Mensajes BUP domicilios {PersonId}: {Messages}", personId, string.Join(';', messageDetails));
            }

            var addressContainer = root;
            if (BupJsonUtils.TryGetProperty(root, out var dataElement, "data") && dataElement.ValueKind == JsonValueKind.Object)
            {
                addressContainer = dataElement;
            }

            if (!BupJsonUtils.TryGetProperty(addressContainer, out var addressesElement, "addresses") || addressesElement.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<BupAddressDto>();
            }

            var addresses = addressesElement
                .EnumerateArray()
                .Where(a => a.ValueKind == JsonValueKind.Object)
                .Select(MapAddress)
                .ToList();

            return OrderByType(addresses);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo domicilios BUP {PersonId}", personId);
            throw;
        }
    }

    private static IEnumerable<BupAddressDto> OrderByType(IEnumerable<BupAddressDto> addresses)
    {
        var priorities = new[] { 1, 2, 6 };

        return addresses
            .OrderBy(address =>
            {
                var index = Array.IndexOf(priorities, address.AddressTypeCode ?? -1);
                return index >= 0 ? index : priorities.Length;
            })
            .ThenBy(address => address.AddressCode ?? int.MaxValue)
            .ToList();
    }

    private static BupAddressDto MapAddress(JsonElement addressElement)
    {
        var address = new BupAddressDto
        {
            AddressCode = BupJsonUtils.GetInt(addressElement, "addressCode", "domicileId", "addressId"),
            AddressTypeCode = BupJsonUtils.GetInt(addressElement, "addressTypeCode", "addressType"),
            AddressTypeName = BupJsonUtils.GetString(addressElement, "addressTypeName"),
            StreetName = BupJsonUtils.GetString(addressElement, "streetName", "street", "calle"),
            StreetNumber = BupJsonUtils.GetString(addressElement, "streetNumber", "number", "altura"),
            Neighborhood = BupJsonUtils.GetString(addressElement, "neighborhood", "barrio"),
            BetweenStreetOne = BupJsonUtils.GetString(addressElement, "betweenStreetOne", "betweenStreet1", "entreCalle1"),
            BetweenStreetTwo = BupJsonUtils.GetString(addressElement, "betweenStreetTwo", "betweenStreet2", "entreCalle2"),
            PostalCode = BupJsonUtils.GetString(addressElement, "postalCode", "zipCode", "codigoPostal"),
            CityName = BupJsonUtils.GetString(addressElement, "cityName", "city"),
            StateName = BupJsonUtils.GetString(addressElement, "stateName", "state"),
            CountryName = BupJsonUtils.GetString(addressElement, "countryName", "country"),
            County = BupJsonUtils.GetString(addressElement, "county"),
            Latitude = BupJsonUtils.GetString(addressElement, "latitudeGeoCoordinate", "latitude"),
            Longitude = BupJsonUtils.GetString(addressElement, "longitudeGeoCoordinate", "longitude")
        };

        if (BupJsonUtils.TryGetProperty(addressElement, out var cityElement, "city") && cityElement.ValueKind == JsonValueKind.Object)
        {
            address.CityName ??= BupJsonUtils.GetString(cityElement, "cityName", "name");
            address.PostalCode ??= BupJsonUtils.GetString(cityElement, "postalCode", "zipCode", "cityPostalCode");

            if (BupJsonUtils.TryGetProperty(cityElement, out var stateElement, "state") && stateElement.ValueKind == JsonValueKind.Object)
            {
                address.StateName ??= BupJsonUtils.GetString(stateElement, "stateName", "name");
            }
        }

        if (BupJsonUtils.TryGetProperty(addressElement, out var countryElement, "country") && countryElement.ValueKind == JsonValueKind.Object)
        {
            address.CountryName ??= BupJsonUtils.GetString(countryElement, "countryName", "name");
        }

        if (BupJsonUtils.TryGetProperty(addressElement, out var typeElement, "addressType") && typeElement.ValueKind == JsonValueKind.Object)
        {
            address.AddressTypeCode ??= BupJsonUtils.GetInt(typeElement, "addressTypeCode", "addressType");
            address.AddressTypeName ??= BupJsonUtils.GetString(typeElement, "contactTypeName", "addressTypeName");
        }

        return address;
    }
}
