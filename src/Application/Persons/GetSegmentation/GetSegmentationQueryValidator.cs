namespace Application.Persons.GetSegmentation;

public class GetSegmentationQueryValidator : AbstractValidator<GetSegmentationQuery>
{
    public GetSegmentationQueryValidator()
    {
        RuleFor(query => query.PersonId)
            .GreaterThan(0)
            .WithMessage("El identificador de la persona debe ser mayor a cero.");
    }
}
