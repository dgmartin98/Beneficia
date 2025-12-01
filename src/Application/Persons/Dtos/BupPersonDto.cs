using System.Collections.Generic;

namespace Application.Persons.Dtos;

public sealed class BupPersonDto
{
    public int? BupId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RegisteredName { get; set; }
    public string? IdentificationNumber { get; set; }
    public string? IdentificationTypeCode { get; set; }
    public string? IdentificationIssuerCountry { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public int? Gender { get; set; }
    public int? PersonType { get; set; }
    public DateTime? BirthDate { get; set; }
    public List<BupPhoneDto> Phones { get; set; } = new();
    public List<BupEmailDto> Emails { get; set; } = new();
    public List<BupAddressDto> Addresses { get; set; } = new();
    public SegmentationDto? Segmentation { get; set; }
}
