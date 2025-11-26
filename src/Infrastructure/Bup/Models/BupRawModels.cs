using System.Collections.Generic;

namespace Infrastructure.Bup.Models;

internal sealed class BupTokenRaw
{
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    public int? expires_in { get; set; }
    public string? scope { get; set; }
}

internal sealed class BupMessageRaw
{
    public string? code { get; set; }
    public string? message { get; set; }
}

internal sealed class BupDocumentRaw
{
    public string? identificationNumber { get; set; }
    public string? identificationTypeCode { get; set; }
    public string? identificationIssuerCountry { get; set; }
}

internal sealed class BupTributaryRaw
{
    public string? taxIdentificationNumber { get; set; }
    public string? taxIdentificationTypeCode { get; set; }
}

internal sealed class BupPersonDataRaw
{
    public int? bupId { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public string? registeredName { get; set; }
    public string? birthDate { get; set; }
    public int? gender { get; set; }
    public int? personType { get; set; }
    public BupDocumentRaw? document { get; set; }
    public BupTributaryRaw? tributaryCode { get; set; }
    public IEnumerable<BupPhoneRaw>? phones { get; set; }
}

internal sealed class BupPersonRootRaw
{
    public IEnumerable<BupMessageRaw>? messages { get; set; }
    public BupPersonDataRaw? data { get; set; }
}

internal sealed class BupPhoneCertificationRaw
{
    public bool? hasWhatsapp { get; set; }
}

internal sealed class BupPhoneRaw
{
    public int? phoneId { get; set; }
    public string? areaPhoneCode { get; set; }
    public string? phoneNumber { get; set; }
    public int? phoneType { get; set; }
    public int? phoneUseType { get; set; }
    public string? countryPhoneCode { get; set; }
    public string? completePhoneNumber { get; set; }
    public BupPhoneCertificationRaw? phoneCertification { get; set; }
}

internal sealed class BupPhonesDataRaw
{
    public IEnumerable<BupPhoneRaw>? phones { get; set; }
}

internal sealed class BupPhonesRootRaw
{
    public IEnumerable<BupMessageRaw>? messages { get; set; }
    public BupPhonesDataRaw? data { get; set; }
}
