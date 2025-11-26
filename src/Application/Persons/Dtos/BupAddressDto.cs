namespace Application.Persons.Dtos;

public sealed class BupAddressDto
{
    public int? AddressCode { get; set; }
    public int? AddressTypeCode { get; set; }
    public string? AddressTypeName { get; set; }
    public string? StreetName { get; set; }
    public string? StreetNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? BetweenStreetOne { get; set; }
    public string? BetweenStreetTwo { get; set; }
    public string? CityName { get; set; }
    public string? StateName { get; set; }
    public string? CountryName { get; set; }
    public string? County { get; set; }
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }
    public string? PostalCode { get; set; }
}
