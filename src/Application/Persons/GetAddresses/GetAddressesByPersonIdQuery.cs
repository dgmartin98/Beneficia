using Application.Persons.Dtos;

namespace Application.Persons.GetAddresses;

public sealed record GetAddressesByPersonIdQuery(int PersonId) : IQuery<IEnumerable<BupAddressDto>>;
