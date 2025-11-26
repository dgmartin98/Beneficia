using System;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using Application.Persons.Dtos;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Application.Services;

namespace Infrastructure.Bup;

public class BupPersonService : Application.Services.IBupPersonService
{
    private readonly IHttpClientFactory _factory;
    private readonly Application.Services.IBupTokenService _tokenService;
    private readonly BupApiOptions _options;
    private readonly ILogger<BupPersonService> _logger;
    private readonly string _httpClientName;

    public BupPersonService(IHttpClientFactory factory, Application.Services.IBupTokenService tokenService, IOptions<BupApiOptions> options, ILogger<BupPersonService> logger, string httpClientName)
    {
        _factory = factory;
        _tokenService = tokenService;
        _options = options.Value;
        _logger = logger;
        _httpClientName = httpClientName;
    }

    public async Task<BupPersonDto> GetPersonByIdAsync(int personId, string? accessToken, CancellationToken cancellationToken)
    {
        if (!_options.HasConfiguredClientId(BupServiceType.Person))
        {
            _logger?.LogInformation("Returning mock person for id {PersonId} because BUP is not configured.", personId);
            return BuildMockPerson(personId);
        }

        try
        {
            if (!_options.HasConfiguredCredentials(BupServiceType.Person))
            {
                if (_options.UseMocksWhenUnconfigured)
                {
                    _logger?.LogInformation("Returning mock person for id {PersonId} because BUP is not configured.", personId);
                    return BuildMockPerson(personId);
                }

                throw new InvalidOperationException("BUP no está configurado (ClientId/ClientSecret). Configure las credenciales para realizar llamadas reales.");
            }

            var token = string.IsNullOrWhiteSpace(accessToken)
                ? await _tokenService.GetTokenAsync(BupServiceType.Person, cancellationToken)
                : accessToken;
            var client = _factory.CreateClient(_httpClientName);
            _logger?.LogInformation("Llamando BUP para persona {PersonId} (catalogo {Catalog}, usuario {Username})", personId, _options.Catalog, _options.GetUsername(BupServiceType.Person));
            using var request = new HttpRequestMessage(HttpMethod.Get, $"people/{personId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-IBM-Client-Id", _options.GetClientId(BupServiceType.Person));
            request.Headers.Add("UserName", _options.GetUsername(BupServiceType.Person));

            var response = await client.SendAsync(request, cancellationToken);
            _logger?.LogInformation("Respuesta BUP persona {PersonId}: {StatusCode}", personId, response.StatusCode);
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
                    throw new InvalidOperationException($"BUP person service returned errors: {string.Join(',', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message"))).Trim(',')}");
                }

                _logger?.LogInformation("Mensajes BUP persona {PersonId}: {Messages}", personId, string.Join(';', messages.Select(m => BupJsonUtils.GetString(m, "code") ?? BupJsonUtils.GetString(m, "message")).Where(m => !string.IsNullOrWhiteSpace(m))));
            }

            if (!BupJsonUtils.TryGetProperty(root, out var dataElement, "data"))
            {
                throw new InvalidOperationException("Respuesta BUP inválida: no se encontró la sección data.");
            }

            var personElement = ResolvePersonElement(dataElement);
            if (personElement.ValueKind != JsonValueKind.Object)
                return null;

            var dto = new BupPersonDto
            {
                BupId = BupJsonUtils.GetInt(personElement, "bupId"),
                FirstName = BupJsonUtils.GetString(personElement, "firstName", "givenName"),
                LastName = BupJsonUtils.GetString(personElement, "lastName", "familyName"),
                RegisteredName = BupJsonUtils.GetString(personElement, "registeredName", "fullName"),
                BirthDate = ParseDate(BupJsonUtils.GetString(personElement, "birthDate")),
                Gender = BupJsonUtils.GetInt(personElement, "gender"),
                PersonType = BupJsonUtils.GetInt(personElement, "personType"),
                IdentificationNumber = ExtractDocumentValue(personElement, "identificationNumber"),
                IdentificationTypeCode = ExtractDocumentValue(personElement, "identificationTypeCode"),
                IdentificationIssuerCountry = ExtractDocumentValue(personElement, "identificationIssuerCountry"),
                TaxIdentificationNumber = ExtractTributaryValue(personElement, "taxIdentificationNumber"),
                Phones = ExtractPhones(personElement, dataElement),
                Emails = ExtractEmails(personElement, dataElement)
            };

            return dto;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error obteniendo persona BUP {PersonId}", personId);

            throw; // rethrow if not in dev fallback
        }
    }

    private static BupPersonDto BuildMockPerson(int personId) => personId switch
    {
        19231437 => new BupPersonDto
        {
            BupId = personId,
            FirstName = "Gabriel",
            LastName = "González",
            RegisteredName = "Gabriel González",
            BirthDate = new DateTime(1985, 4, 12),
            Gender = 1,
            PersonType = 1,
            IdentificationNumber = "19231437",
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = "20-12345678-9"
        },
        244885 => new BupPersonDto
        {
            BupId = personId,
            FirstName = "María",
            LastName = "Pérez",
            RegisteredName = "María Pérez",
            BirthDate = new DateTime(1990, 7, 3),
            Gender = 2,
            PersonType = 1,
            IdentificationNumber = "244885",
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = "27-87654321-0"
        },
        _ => new BupPersonDto
        {
            BupId = personId,
            FirstName = "Dev",
            LastName = "User",
            RegisteredName = "Dev User",
            BirthDate = null,
            Gender = null,
            PersonType = 1,
            IdentificationNumber = personId.ToString(),
            IdentificationTypeCode = "DNI",
            IdentificationIssuerCountry = "AR",
            TaxIdentificationNumber = null
        }
    };

    private static DateTime? ParseDate(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        if (DateTime.TryParse(s, out var dt))
            return dt;
        return null;
    }

    private static JsonElement ResolvePersonElement(JsonElement dataElement)
    {
        if (dataElement.ValueKind == JsonValueKind.Object)
        {
            if (BupJsonUtils.TryGetProperty(dataElement, out var nested, "person", "persona"))
                return nested;

            if (BupJsonUtils.TryGetProperty(dataElement, out var personArray, "people") && personArray.ValueKind == JsonValueKind.Array)
            {
                return personArray.EnumerateArray().FirstOrDefault();
            }

            return dataElement;
        }

        if (dataElement.ValueKind == JsonValueKind.Array)
            return dataElement.EnumerateArray().FirstOrDefault();

        return default;
    }

    private static string? ExtractDocumentValue(JsonElement personElement, string propertyName)
    {
        if (BupJsonUtils.TryGetProperty(personElement, out var documentElement, "document") && documentElement.ValueKind == JsonValueKind.Object)
        {
            return BupJsonUtils.GetString(documentElement, propertyName);
        }

        return BupJsonUtils.GetString(personElement, propertyName);
    }

    private static string? ExtractTributaryValue(JsonElement personElement, string propertyName)
    {
        if (BupJsonUtils.TryGetProperty(personElement, out var tributaryElement, "tributaryCode", "tributary" ) && tributaryElement.ValueKind == JsonValueKind.Object)
        {
            return BupJsonUtils.GetString(tributaryElement, propertyName);
        }

        return BupJsonUtils.GetString(personElement, propertyName);
    }

    private static List<BupPhoneDto> ExtractPhones(JsonElement personElement, JsonElement dataElement)
    {
        JsonElement phonesElement = default;

        if (!BupJsonUtils.TryGetProperty(personElement, out phonesElement, "phones"))
        {
            BupJsonUtils.TryGetProperty(dataElement, out phonesElement, "phones");
        }

        if (phonesElement.ValueKind != JsonValueKind.Array)
            return new List<BupPhoneDto>();

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
            .ToList()
            .OrderBy(phone =>
            {
                var priorities = new[] { 1, 2, 6 };
                var index = Array.IndexOf(priorities, phone.PhoneUseType ?? -1);
                return index >= 0 ? index : priorities.Length;
            })
            .ThenBy(phone => phone.PhoneId ?? int.MaxValue)
            .ToList();
    }

    private static List<BupEmailDto> ExtractEmails(JsonElement personElement, JsonElement dataElement)
    {
        JsonElement emailsElement = default;

        if (!BupJsonUtils.TryGetProperty(personElement, out emailsElement, "emails"))
        {
            BupJsonUtils.TryGetProperty(dataElement, out emailsElement, "emails");
        }

        if (emailsElement.ValueKind != JsonValueKind.Array)
            return new List<BupEmailDto>();

        var priorities = new[] { 1, 2, 6 };

        return emailsElement
            .EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.Object)
            .Select(e => new BupEmailDto
            {
                EmailId = BupJsonUtils.GetInt(e, "emailId"),
                Email = BupJsonUtils.GetString(e, "email"),
                EmailUseType = BupJsonUtils.GetInt(e, "emailUseType")
            })
            .OrderBy(email =>
            {
                var index = Array.IndexOf(priorities, email.EmailUseType ?? -1);
                return index >= 0 ? index : priorities.Length;
            })
            .ThenBy(email => email.EmailId ?? int.MaxValue)
            .ToList();
    }

    private static bool ExtractHasWhatsapp(JsonElement phoneElement)
    {
        if (BupJsonUtils.TryGetProperty(phoneElement, out var certificationElement, "phoneCertification", "certification") && certificationElement.ValueKind == JsonValueKind.Object)
        {
            return BupJsonUtils.GetBool(certificationElement, "hasWhatsapp");
        }

        return BupJsonUtils.GetBool(phoneElement, "hasWhatsapp");
    }
}
