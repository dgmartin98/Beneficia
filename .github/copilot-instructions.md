# Copilot Instructions

## Estilo de Código

### Nomenclatura

- **PascalCase**: Clases, métodos, propiedades, eventos
- **camelCase**: Parámetros, variables locales
- **\_camelCase**: Campos privados de instancia
- **Interfaces**: Prefijo `I`
- **Nombres en español** para variables y métodos (excepto librerías estándar)

### Formateo y C#

- 4 espacios indentación, llaves en nuevas líneas, límite 140 caracteres
- Usa `var`, expresiones de colección `[]`, coincidencia de patrones `is`
- Namespace con scope de archivo, tipos nullable habilitados
- Métodos async con sufijo `Async`, usar `CancellationToken`

### Manejo de Errores - Patrón Result (OBLIGATORIO)

- **NUNCA** excepciones para lógica de negocio. Se utiliza la lib `Gss.Results` que se encuentra ya incluid en la lib `Gss.MinimalApis.Mediator`.
- Handlers usan `ICommandHandler<>`, `IQueryHandler<>`, `IPagedQueryHandler<>` con `Result`
  - `Result.Success()` / `Result<T>.Success(value)` para éxito
  - `Result.Failure(mensaje, codigo)` para errores de negocio
  - `Result.NotFound("<mensaje>")` para errores de no encontrado
  - `Result.Conflict("<mensaje>")` para conflictos
  - `Result.Forbidden()` para acceso denegado
  - `Result.ValidationFailure(errores)` para validaciones
  - **Paginación**: Usar `PagedResult<T>.Create(items, totalCount)` para consultas paginadas
- Endpoints pueden usar `StandardResults`. (Success, ValidationError, BusinessError, Forbidden, etc) (Gss.MinimalApis.Models)

### Monolitos Modulares

Si consideras que de acuerdo al pedido del usuario necesitas crear un nuevo módulo, comunicaselo al desarrollador para que el te lo confirme y sigue las instrucciones en [Monolito modular](./instructions/monolito-modular.instructions.md).

## Generación de Features con Gss.Mediator que se encuentra incluida en la lib `Gss.MinimalApis.Mediator`

### Estructura Obligatoria

**Application/{Entidad}/{Feature}/** (sin subcarpetas Commands/Queries):

- `{Operacion}{Entidad}Command.cs` / `{Operacion}{Entidad}Query.cs` / `{Operacion}{Entidad}PagedQuery.cs` (si aplica) debe ser `record`. Generar `{Operacion}{Entidad}Validator.cs` (FluentValidation) dentro del mismo archivo, al final.
- `{Operacion}{Entidad}CommandHandler.cs` / `{Operacion}{Entidad}QueryHandler.cs` / `{Operacion}{Entidad}PagedQueryHandler.cs` (si aplica).
- `{Operacion}{Entidad}Dto.cs`. Siempre se devuelve un `Result<Dto>`. Si es una consulta paginada se devuelve `PagedResult<Dto>`. (si aplica, por ejemplo para consultas paginadas). Si no se necesita devolver datos, se devuelve `Result` y no se necesita este archivo.
- `{Entidad}/{Feature}/{Operacion}Endpoint.cs` utilizando `IEndpoint` (MapEndpoint)
  - Generarlo siempre que se necesite un endpoint.

### Validators

- FluentValidation
- No usar .WithMessage a menos que se lo requiera. Por defecto, mensajes predeterminados.

### Handlers (OBLIGATORIO)

```csharp
// Sin retorno
public class CrearUsuarioCommandHandler : ICommandHandler<CrearUsuarioCommand>
{
    private readonly IAppDbContext _context; // Si se utiliza EF Core

    public async Task<Result> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
    {
        // Validaciones básicas al Validator, solo aplicar posibles validaciones de negocio aquí
        var usuarioExistente = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (usuarioExistente != null)
            return Result.Conflict("El email ya está registrado", "EMAIL_DUPLICADO");

        var usuario = new Usuario { ... };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// Con retorno
public class ObtenerUsuarioQueryHandler : IQueryHandler<ObtenerUsuarioQuery, UsuarioResponse>
{
    private readonly IAppDbContext _context; // Si se utiliza EF Core

    public async Task<Result<UsuarioResponse>> Handle(ObtenerUsuarioQuery request, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (usuario == null)
            return Result<UsuarioResponse>.NotFound("Usuario no encontrado");

        return Result<UsuarioResponse>.Success(new UsuarioResponse { ... });
    }
}

// Consulta paginada

public class ObtenerUsuariosQueryHandler : IPagedQueryHandler<ObtenerUsuariosQuery, UsuarioDto>
{
    private readonly IAppDbContext _context; // Si se utiliza EF Core

    public ObtenerUsuariosQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Maneja la obtención de una lista paginada de usuarios.
    /// </summary>
    /// <param name="request">Consulta que contiene los parámetros de paginación y búsqueda.</param>
    /// <param name="cancellationToken">Token para cancelar la operación de forma asíncrona.</param>
    /// <returns>Resultado con la lista paginada de usuarios.</returns>
    public async Task<PagedResult<UsuarioDto>> Handle(ObtenerUsuariosQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Usuarios.AsQueryable();
        // para filtros simples usar .Where sencillo, para consultas complejas usar PredicateBuilder.True<Usuario>();
        // aplicar filtros y ordenamiento...
        query = query.Where(predicado).OrderBy(u => u.Nombre);

        var totalTask = query.CountAsync(cancellationToken);
        var usuariosTask = query.GetPage(request.Pagina, request.ItemsPorPagina).ToListAsync(cancellationToken);
        await Task.WhenAll(totalTask, usuariosTask);
        var usuarios = await usuariosTask;
        var total = await totalTask;
        return PagedResult<UsuarioDto>.Create(usuarios.Select(u => u.ToDto()), total);
    }
}

```

### Endpoints (OBLIGATORIO)

Estos métodos de extensión ya manejan las respuestas exitosas y de error de los Result de los Handlers.

```csharp
var resultado = await mediator.Send(command);
return resultado.ToNoContentResult();
return resultado.ToCreatedResult();
return resultado.ToPagedResult(pagina, itemsPorPagina);
return resultado.ToHttpResult();
return
```

Además, del código del método del endpoint, y los summary y description establecer para cada salida un Produces diferente:

```csharp
app.Map{HttpMethod}("/{entidad-plural}", {Funcion})
    .WithName("{Funcion}")
    .WithSummary("{Resumen Funcion}")
    .WithDescription("{Descripcion Funcion}")
    .WithTags("{Entidad}")
    .ProducesCreated<Guid>(); // MapPost
    .ProducesNoContent(); // MapPut / MapDelete
    .ProducesOk<{Operacion}Dto>(); // MapGet
    .ProducesPage<{Operacion}Dto>(); // MapGet para consultas paginadas.
```

### Acceso a Datos

- Si se utiliza EF Core, usar las reglas definidas en [reglas EF Core](./instructions/db/efcore.instructions.md)
- Si se utiliza Dapper, usar las reglas definidas en [reglas Dapper](./instructions/db/dapper-storedprocedures.instructions.md)
- Si se utiliza una combinación de ambos, seguir ambas reglas.

EFCore QueryFilters para SoftDelete en el caso de las entidades auditables.

### Extensiones de `IQueryable`

- Utiliza `Gss.Linq` para consultas complejas sobre `IQueryable`:
  - `.WhereIf<T>(condition, predicate)`. Aplica filtro solo si la condición se cumple
  - `.WhereIfNotNull<T, TValue>(TValue? value, predicate)`. Aplica filtro solo si el valor especificado no es nulo.
  - `.WhereAll<T>(predicates[])`. Aplica múltiples filtros a la consulta
  - `.WhereAny<T>(predicates[])`. Aplica al menos un filtro a la consulta
  - `.GetPage(pagina, itemsPorPagina)`. Aplica offset y limit a una consulta.

### Servicios Externos

- **REST**: Interfaces Refit en `Infrastructure/ExternalServices/{Service}/{Service}Client.cs`
- **SOAP**: Clases en `Infrastructure/ExternalServices/{Service}Reference/{Service}Service.cs`. utilizar dotnet svc-util
- Registrar en DI container (`Api/Program.cs`) usando un metodo de extensión descripto en `Infrastructure/ExternalServicesInstaller.cs`

### Tests Unitarios

- Funcionalidades principales (lógica de negocio). Priorizar Handlers y si el usuario lo pide, validators. Si no existen handlers, solo endpoints.
- **Ubicación**: `test/{Proyecto}.Tests/`
- **Naming**: `{Feature}Tests`, método `Metodo_Escenario_Resultado`.
- **Framework**: xUnit + Shouldly + NSubstitute + Bogus para datos fake.
- **Patrón AAA**: Arrange-Act-Assert
- Si se utiliza EF Core, usar las reglas definidas en [reglas test EF Core](./instructions/test/tests.efcore.instructions.md)
- Si se utiliza Dapper, usar las reglas definidas en [reglas test Dapper](./instructions/test/tests.dapper.instructions.md)
- Si se utiliza una combinación de ambos, seguir ambas reglas.
