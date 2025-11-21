namespace Application.Persons;

/// <summary>
/// DTO expuesto por la API para representar a una persona con estructura extendida.
/// </summary>
public class PersonResponse
{
    public SecDto Sec { get; set; } = new();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string RegisteredName { get; set; } = string.Empty;
    public DocumentDto Document { get; set; } = new();
    public TributaryCodeDto TributaryCode { get; set; } = new();
    public int Gender { get; set; }
    public int PersonType { get; set; }
    public DateTime BirthDate { get; set; }
    public List<EmailDto> Emails { get; set; } = new();
    public List<PhoneDto> Phones { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
    public List<PolicyDto> Policies { get; set; } = new();
}

public class SecDto
{
    public int Estrellas { get; set; }
    public string Primas { get; set; } = string.Empty;
    public int MixProductos { get; set; }
    public int Antiguedad { get; set; }
}

public class DocumentDto
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string IdentificationypeCode { get; set; } = string.Empty;
    public string IdentificationIssuerCountry { get; set; } = string.Empty;
}

public class TributaryCodeDto
{
    public string TaxIdentificationNumber { get; set; } = string.Empty;
    public string TaxIdentificationTypeCode { get; set; } = string.Empty;
}

public class EmailDto
{
    public string Email { get; set; } = string.Empty;
    public int MailId { get; set; }
}

public class PhoneDto
{
    public string AreaCode { get; set; } = string.Empty;
    public int PhoneId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public int PhoneType { get; set; }
}

public class AddressDto
{
    public int AddressCode { get; set; }
    public string StreetName { get; set; } = string.Empty;
    public int StreetNumber { get; set; }
    public string BetweenStreetOne { get; set; } = string.Empty;
    public string BetweenStreetTwo { get; set; } = string.Empty;
    public CityDto City { get; set; } = new();
    public CountryDto Country { get; set; } = new();
}

public class CityDto
{
    public int CityCode { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string CityPostalCode { get; set; } = string.Empty;
    public StateDto State { get; set; } = new();
}

public class StateDto
{
    public int StateCode { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string StateIsoCode { get; set; } = string.Empty;
}

public class CountryDto
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
}

public class PolicyDto
{
    public string? ReferenceNumber { get; set; }
    public int? CertificateId { get; set; }
    public int? CertificateNumber { get; set; }
    public object? BranchCode { get; set; }
    public string? BranchName { get; set; }
    public object? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public object? PolicyTypeCode { get; set; }
    public string? PolicyTypeName { get; set; }
    public bool? IsVigent { get; set; }
    public object? PolicyGroupTypeCode { get; set; }
    public string? PolicyGroupTypeName { get; set; }
    public object? StadisticalCode { get; set; }
    public object? StadisticalGroup { get; set; }
    public string? StadisticalGroupName { get; set; }
    public DateTime? PolicyPeriodStartEffectiveDate { get; set; }
    public DateTime? PolicyPeriodEndEffectiveDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public object? StatusCode { get; set; }
    public string? StatusName { get; set; }
    public object? OrganizerNumber { get; set; }
    public object? ProducerNumber { get; set; }
    public HealthPlanDto? HealthPlan { get; set; }
}

public class HealthPlanDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
