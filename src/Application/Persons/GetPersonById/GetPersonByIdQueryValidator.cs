namespace Application.Persons.GetPersonById;

/// <summary>
/// Validador para la query <see cref="GetPersonByIdQuery"/>.
/// </summary>
public class GetPersonByIdQueryValidator : AbstractValidator<GetPersonByIdQuery>
{
    public GetPersonByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la persona es requerido.");
    }
}
