using Application.Persons.Dtos;
using Gss.Mediator;

namespace Application.Persons.GetEmails;

public sealed record GetEmailsByPersonIdQuery(int PersonId) : IQuery<IEnumerable<BupEmailDto>>;
