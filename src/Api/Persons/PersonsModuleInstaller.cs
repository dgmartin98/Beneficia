using Application.Persons.GetPersonById;
using FluentValidation;
using Infrastructure.Persons;

namespace Api.Persons;

/// <summary>
/// Instalador del módulo Persons. Registra dependencias verticales para la slice.
/// </summary>
public static class PersonsModuleInstaller
{
    public static IServiceCollection AddPersonsModule(this IServiceCollection services)
    {
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Los validadores se registran aquí para que el pipeline de FluentValidation los descubra.
        services.AddValidatorsFromAssemblyContaining<GetPersonByIdQueryValidator>();

        // Los handlers y endpoints se registran automáticamente por AddGssMediator y MapEndpointsFromAssembly.
        // Este método deja la intención documentada dentro del módulo Persons.
        return services;
    }
}
