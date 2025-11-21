namespace Application.Persons.GetPersonById;

/// <summary>
/// Validador para la query <see cref="GetPersonByIdQuery"/>.
/// </summary>
public class GetPersonByIdQueryValidator : AbstractValidator<GetPersonByIdQuery>
{
    public GetPersonByIdQueryValidator()
    {
        RuleFor(query => query.PersonId)
            .GreaterThan(0)
            .WithMessage("El identificador de la persona debe ser mayor a cero.");
    }
}
