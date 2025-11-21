namespace Application.Persons.Dtos;

public sealed class BupPhoneDto
{
    public int? PhoneId { get; set; }
    public string? AreaPhoneCode { get; set; }
    public string? PhoneNumber { get; set; }
    public int? PhoneType { get; set; }
    public int? PhoneUseType { get; set; }
    public string? CountryPhoneCode { get; set; }
    public string? CompletePhoneNumber { get; set; }
    public bool HasWhatsapp { get; set; }
}
