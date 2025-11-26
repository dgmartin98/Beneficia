using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using Application.Persons.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Application.Services;

namespace Infrastructure.Bup;

public class BupPhoneService : Application.Services.IBupPhoneService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly ILogger<BupPhoneService> _logger;
    private readonly string _httpClientName;

    public BupPhoneService(IHttpClientFactory factory, Application.Services.IBupTokenService tokenService, IOptions<BupApiOptions> options, ILogger<BupPhoneService> logger, string httpClientName)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _httpClientName = httpClientName;
    }

    public async Task<IEnumerable<BupPhoneDto>> GetPhonesByPersonIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        if (!_options.HasConfiguredClientId(BupServiceType.Phones))
        {
            _logger?.LogInformation("Returning mock phones for id {PersonId} because BUP is not configured.", personId);
            return BuildMockPhones(personId);
        }

        try
        {
            if (!_options.HasConfiguredCredentials(BupServiceType.Phones))
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning mock phones for id {PersonId} because BUP is not configured.", personId);
                    return BuildMockPhones(personId);
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para obtener teléfonos reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(BupServiceType.Phones, cancellationToken)
                : accessToken;
            var client = _factory.CreateClient(_httpClientName);
            _logger?.LogInformation("Llamando BUP phones para persona {PersonId} (catalogo {Catalog}, usuario {Username})", personId, _options.Catalog, _options.GetUsername(BupServiceType.Phones));
            using var request = new HttpRequestMessage(HttpMethod.Get, $"people/{personId}/phones");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.GetClientId(BupServiceType.Phones));
            request.Headers.Add("UserName", _options.GetUsername(BupServiceType.Phones));

            var response = await client.SendAsync(request, cancellationToken);
            _logger?.LogInformation("Respuesta BUP phones {PersonId}: {StatusCode}", personId, response.StatusCode);
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
                    throw new InvalidOperationException($"BUP phones service returned errors: {string.Join(',', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message"))).Trim(',')}");
                }

                _logger?.LogInformation("Mensajes BUP phones {PersonId}: {Messages}", personId, string.Join(';', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message")).Where(m => !string.IsNullOrWhiteSpace(m))));
            }

            JsonElement dataElement;
            if (!BupJsonUtils.TryGetProperty(root, out dataElement, "data"))
            {
                throw new InvalidOperationException("Respuesta BUP phones inválida: no se encontró la sección data.");
            }

            if (!BupJsonUtils.TryGetProperty(dataElement, out var phonesElement, "phones"))
            {
                return Enumerable.Empty<BupPhoneDto>();
            }

            if (phonesElement.ValueKind != JsonValueKind.Array)
            {
                return Enumerable.Empty<BupPhoneDto>();
            }

            return phonesElement
                .EnumerateArray()
                .Where(p => p.ValueKind == JsonValueKind.Object)
                .Select(p => new BupPhoneDto
                {
                    PhoneId = BupJsonUtils.GetInt(p, "phoneId"),
                    AreaPhoneCode = BupJsonUtils.GetString(p, "areaPhoneCode"),
                    PhoneNumber = BupJsonUtils.GetString(p, "phoneNumber"),
                    PhoneType = BupJsonUtils.GetInt(p, "phoneType"),
                    PhoneUseType = BupJsonUtils.GetInt(p, "phoneUseType"),
                    CountryPhoneCode = BupJsonUtils.GetString(p, "countryPhoneCode"),
                    CompletePhoneNumber = BupJsonUtils.GetString(p, "completePhoneNumber"),
                    HasWhatsapp = ExtractHasWhatsapp(p)
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo phones BUP {PersonId}", personId);

            throw;
        }
    }

    private static IEnumerable<BupPhoneDto> BuildMockPhones(int personId) => personId switch
    {
        19231437 => new List<BupPhoneDto>
        {
            new BupPhoneDto { PhoneId = 1, AreaPhoneCode = "11", PhoneNumber = "12345678", CompletePhoneNumber = "+54 9 11 1234-5678", CountryPhoneCode = "54", PhoneType = 1, PhoneUseType = 1, HasWhatsapp = true },
        },
        244885 => new List<BupPhoneDto>
        {
            new BupPhoneDto { PhoneId = 2, AreaPhoneCode = "11", PhoneNumber = "87654321", CompletePhoneNumber = "+54 11 8765-4321", CountryPhoneCode = "54", PhoneType = 2, PhoneUseType = 1, HasWhatsapp = false },
        },
        _ => new List<BupPhoneDto>()
    };

    private static bool ExtractHasWhatsapp(JsonElement phoneElement)
    {
        if (BupJsonUtils.TryGetProperty(phoneElement, out var certificationElement, "phoneCertification", "certification") && certificationElement.ValueKind == JsonValueKind.Object)
        {
            return BupJsonUtils.GetBool(certificationElement, "hasWhatsapp");
        }

        return BupJsonUtils.GetBool(phoneElement, "hasWhatsapp");
    }
}
