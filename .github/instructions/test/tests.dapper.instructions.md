--- 
applyTo: test/**/*.cs
--- 

# Instrucciones para Tests Unitarios con ADO.NET + Dapper

## Principios Generales
- **Prioridad**: Handlers > Clases SQL > Validators > Endpoints
- **Patrón AAA**: Arrange-Act-Assert
- **Tests unitarios**: Mock `IAdoRepository` para handlers
- **Tests integración**: Base de datos real para clases SQL

## Estructura y Nomenclatura
- **Ubicación**: `test/{Proyecto}.Tests/{Entidad}/`
- **Clase**: `{Operacion}{Entidad}Tests`
- **Método**: `{Accion}_{Escenario}_{Resultado}`

## Dependencias
```xml
<PackageReference Include="xunit" />
<PackageReference Include="Shouldly" />
<PackageReference Include="NSubstitute" />
<PackageReference Include="Bogus" />
<PackageReference Include="Microsoft.Data.SqlClient" />
<PackageReference Include="Testcontainers.MsSql" />
```

```csharp
// GlobalUsings.cs
global using Gss.Results;
global using Gss.CorporateApps.Data.Ado.Repositories;
global using Shouldly;
global using Xunit;
global using NSubstitute;
global using Bogus;
```

## Tests de Handlers (OBLIGATORIO)

### Command Handlers
```csharp
public class {Crear}{Entidad}CommandHandlerTests : IDisposable
{
    private readonly IAdoRepository _repository;
    private readonly {Crear}{Entidad}CommandHandler _handler;
    private readonly Faker _faker;

    public {Crear}{Entidad}CommandHandlerTests()
    {
        _repository = Substitute.For<IAdoRepository>();
        _handler = new {Crear}{Entidad}CommandHandler(_repository);
        _faker = new Faker("es");
    }

    [Fact]
    public async Task Handle_{Entidad}Valido_DeberiaCrear{Entidad}Exitosamente()
    {
        // Arrange
        var command = new {Crear}{Entidad}Command { {Propiedad} = _faker.{FakerProperty} };
        _repository.ExecuteCommandAsync(Arg.Any<{Crear}{Entidad}SqlCommand>())
            .Returns(Task.FromResult(Guid.NewGuid()));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).ExecuteCommandAsync(Arg.Any<{Crear}{Entidad}SqlCommand>());
    }

    [Fact]
    public async Task Handle_{Campo}Duplicado_DeberiaRetornarConflict()
    {
        // Arrange
        var command = new {Crear}{Entidad}Command { {Campo} = "{valor}" };
        _repository.GetFirstAsync(Arg.Any<{Validar}{Entidad}ExistenteSql>())
            .Returns(Task.FromResult(true));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeFalse();
        resultado.ErrorCode.ShouldBe("{CODIGO_ERROR}");
        await _repository.DidNotReceive().ExecuteCommandAsync(Arg.Any<{Crear}{Entidad}SqlCommand>());
    }
}
```
```csharp
public class CrearUsuarioCommandHandlerTests : IDisposable
{
    private readonly IAdoRepository _repository;
    private readonly CrearUsuarioCommandHandler _handler;
    private readonly string _connectionString;
    private readonly Faker _faker;

    public CrearUsuarioCommandHandlerTests()
    {
        // Usar conexión a base de datos de prueba o TestContainers
        _connectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;";
        _repository = Substitute.For<IAdoRepository>();
        _handler = new CrearUsuarioCommandHandler(_repository);
        _faker = new Faker("es");
    }

    [Fact]
    public async Task Handle_UsuarioValido_DeberiaCrearUsuarioExitosamente()
    {
        // Arrange
        var command = new CrearUsuarioCommand
        {
            Nombre = _faker.Person.FirstName,
            Email = _faker.Internet.Email()
        };

        var comandoSql = new CrearUsuarioSqlCommand(command.Nombre, command.Email);
        var usuarioId = Guid.NewGuid();

        _repository.ExecuteCommandAsync(Arg.Any<CrearUsuarioSqlCommand>())
            .Returns(Task.FromResult(usuarioId));

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).ExecuteCommandAsync(Arg.Any<CrearUsuarioSqlCommand>());
    }

    [Fact]
    public async Task Handle_EmailDuplicado_DeberiaRetornarConflict()
    {
        // Arrange
        var command = new CrearUsuarioCommand
        {
            Nombre = _faker.Person.FirstName,
            Email = "usuario@existente.com"
        };

        var validacionSql = new ValidarUsuarioExistenteSql(command.Email);
        
        _repository.GetFirstAsync(Arg.Any<ValidarUsuarioExistenteSql>())
            .Returns(Task.FromResult(true)); // Usuario ya existe

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeFalse();
        resultado.ErrorCode.ShouldBe("EMAIL_DUPLICADO");
        resultado.ErrorMessage.ShouldBe("El email ya está registrado");
        
        // Verificar que no se intentó crear el usuario
        await _repository.DidNotReceive().ExecuteCommandAsync(Arg.Any<CrearUsuarioSqlCommand>());
    }

    [Fact]
    public async Task Handle_ErrorEnBaseDeDatos_DeberiaRetornarError()
    {
        // Arrange
        var command = new CrearUsuarioCommand
        {
            Nombre = _faker.Person.FirstName,
            Email = _faker.Internet.Email()
        };

        _repository.ExecuteCommandAsync(Arg.Any<CrearUsuarioSqlCommand>())
            .Throws(new SqlException("Error de conexión"));

        // Act & Assert
        await Should.ThrowAsync<SqlException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    public void Dispose()
    {
        // Limpiar recursos si es necesario
    }
}
```

#### Query Handlers con Stored Procedures
```csharp
public class ObtenerUsuarioQueryHandlerTests : IDisposable
{
    private readonly IAdoRepository _repository;
    private readonly ObtenerUsuarioQueryHandler _handler;
    private readonly Faker _faker;

    public ObtenerUsuarioQueryHandlerTests()
    {
        _repository = Substitute.For<IAdoRepository>();
        _handler = new ObtenerUsuarioQueryHandler(_repository);
        _faker = new Faker("es");
    }

    [Fact]
    public async Task Handle_UsuarioExiste_DeberiaRetornarUsuario()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuarioDto = new UsuarioDto 
        { 
            Id = usuarioId, 
            Nombre = "Juan Pérez",
            Email = "juan@test.com"
        };

        var consultaSql = new ObtenerUsuarioSqlSingleQuery(usuarioId);
        
        _repository.GetFirstAsync(Arg.Any<ObtenerUsuarioSqlSingleQuery>())
            .Returns(Task.FromResult(usuarioDto));

        var query = new ObtenerUsuarioQuery { Id = usuarioId };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldNotBeNull();
        resultado.Value.Id.ShouldBe(usuarioId);
        resultado.Value.Nombre.ShouldBe("Juan Pérez");
        
        await _repository.Received(1).GetFirstAsync(Arg.Any<ObtenerUsuarioSqlSingleQuery>());
    }

    [Fact]
    public async Task Handle_UsuarioNoExiste_DeberiaRetornarNotFound()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        
        _repository.GetFirstAsync(Arg.Any<ObtenerUsuarioSqlSingleQuery>())
            .Returns(Task.FromResult<UsuarioDto>(null));

        var query = new ObtenerUsuarioQuery { Id = usuarioId };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeFalse();
        resultado.ErrorMessage.ShouldBe("Usuario no encontrado");
    }

    [Fact]
    public async Task Handle_ErrorEnConsulta_DeberiaLanzarExcepcion()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        
        _repository.GetFirstAsync(Arg.Any<ObtenerUsuarioSqlSingleQuery>())
            .Throws(new SqlException("Error en la consulta"));

        var query = new ObtenerUsuarioQuery { Id = usuarioId };

        // Act & Assert
        await Should.ThrowAsync<SqlException>(() => 
            _handler.Handle(query, CancellationToken.None));
    }

    public void Dispose()
    {
        // Limpiar recursos si es necesario
    }
}
```

#### Paged Query Handlers con Stored Procedures
```csharp
public class ObtenerUsuariosQueryHandlerTests : IDisposable
{
    private readonly IAdoRepository _repository;
    private readonly ObtenerUsuariosQueryHandler _handler;
    private readonly Faker _faker;

    public ObtenerUsuariosQueryHandlerTests()
    {
        _repository = Substitute.For<IAdoRepository>();
        _handler = new ObtenerUsuariosQueryHandler(_repository);
        _faker = new Faker("es");
    }

    [Fact]
    public async Task Handle_ConsultaValida_DeberiaRetornarResultadoPaginado()
    {
        // Arrange
        var usuariosDto = GenerarUsuariosDtoFake(5);
        var totalCount = 10;

        var consultaSql = new ObtenerUsuariosSqlQuery(1, 5, null, null);
        
        _repository.QueryAsync(Arg.Any<ObtenerUsuariosSqlQuery>())
            .Returns(Task.FromResult(usuariosDto.AsEnumerable()));

        // Simular consulta de conteo separada si es necesario
        var conteoSql = new ContarUsuariosSqlSingleQuery(null, null);
        _repository.GetFirstAsync(Arg.Any<ContarUsuariosSqlSingleQuery>())
            .Returns(Task.FromResult(totalCount));

        var query = new ObtenerUsuariosQuery { Pagina = 1, ItemsPorPagina = 5 };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Items.Count().ShouldBe(5);
        resultado.TotalCount.ShouldBe(10);
        resultado.CurrentPage.ShouldBe(1);
        
        await _repository.Received(1).QueryAsync(Arg.Any<ObtenerUsuariosSqlQuery>());
    }

    [Fact]
    public async Task Handle_FiltroNombre_DeberiaFiltrarCorrectamente()
    {
        // Arrange
        var usuariosFiltrados = new List<UsuarioDto>
        {
            new UsuarioDto { Id = Guid.NewGuid(), Nombre = "Juan Pérez", Email = "juan@test.com" }
        };

        var consultaSql = new ObtenerUsuariosSqlQuery(1, 10, "Juan", null);
        
        _repository.QueryAsync(Arg.Any<ObtenerUsuariosSqlQuery>())
            .Returns(Task.FromResult(usuariosFiltrados.AsEnumerable()));

        var conteoSql = new ContarUsuariosSqlSingleQuery("Juan", null);
        _repository.GetFirstAsync(Arg.Any<ContarUsuariosSqlSingleQuery>())
            .Returns(Task.FromResult(1));

        var query = new ObtenerUsuariosQuery 
        { 
            Pagina = 1, 
            ItemsPorPagina = 10,
            Nombre = "Juan"
        };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Items.Count().ShouldBe(1);
        resultado.Items.First().Nombre.ShouldBe("Juan Pérez");
        
        await _repository.Received(1).QueryAsync(Arg.Is<ObtenerUsuariosSqlQuery>(
            q => q.Nombre == "Juan"));
    }

    [Fact]
    public async Task Handle_ConsultaVacia_DeberiaRetornarListaVacia()
    {
        // Arrange
        var usuariosVacios = new List<UsuarioDto>();
        
        _repository.QueryAsync(Arg.Any<ObtenerUsuariosSqlQuery>())
            .Returns(Task.FromResult(usuariosVacios.AsEnumerable()));

        _repository.GetFirstAsync(Arg.Any<ContarUsuariosSqlSingleQuery>())
            .Returns(Task.FromResult(0));

        var query = new ObtenerUsuariosQuery { Pagina = 1, ItemsPorPagina = 10 };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Items.Count().ShouldBe(0);
        resultado.TotalCount.ShouldBe(0);
    }

    private List<UsuarioDto> GenerarUsuariosDtoFake(int cantidad)
    {
        var faker = new Faker<UsuarioDto>("es")
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Nombre, f => f.Person.FirstName)
            .RuleFor(u => u.Email, f => f.Internet.Email());
        
        return faker.Generate(cantidad);
    }

    public void Dispose()
    {
        // Limpiar recursos si es necesario
    }
}
```

## Tests de Clases SQL con Dapper

### Tests para SqlCommand (CommandResult<T>)
```csharp
public class {Crear}{Entidad}SqlCommandTests : IDisposable
{
    private readonly IAdoRepository _repository;

    public {Crear}{Entidad}SqlCommandTests()
    {
        _repository = new AdoRepository("{connectionString}");
    }

    [Fact]
    public async Task Execute_ParametrosValidos_DeberiaEjecutarComando()
    {
        // Arrange
        var comando = new {Crear}{Entidad}SqlCommand("{param1}", "{param2}");

        // Act
        var resultado = await _repository.ExecuteCommandAsync(comando);

        // Assert
        resultado.ShouldNotBe(Guid.Empty);
    }
}
```

### Tests para SqlSingleQuery (SingleResult<T>)
```csharp
public class {Obtener}{Entidad}SqlSingleQueryTests : IDisposable
{
    [Fact]
    public async Task GetResult_{Entidad}Existe_DeberiaRetornar{Entidad}()
    {
        // Arrange
        var {entidad}Id = await Crear{Entidad}Prueba("{param1}", "{param2}");
        var consulta = new {Obtener}{Entidad}SqlSingleQuery({entidad}Id);

        // Act
        var resultado = await _repository.GetFirstAsync(consulta);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Id.ShouldBe({entidad}Id);
    }
}
```

### Tests para SqlQuery (Query<T>)
```csharp
public class {Obtener}{Entidades}SqlQueryTests : IDisposable
{
    [Fact]
    public async Task GetResult_SinFiltros_DeberiaRetornarTodos()
    {
        // Arrange
        await Crear{Entidades}Prueba();
        var consulta = new {Obtener}{Entidades}SqlQuery(1, 10, null);

        // Act
        var resultado = await _repository.QueryAsync(consulta);

        // Assert
        resultado.ShouldNotBeNull();
        resultado.Count().ShouldBeGreaterThan(0);
    }
}
```
## Tests de Validators
```csharp
public class {Crear}{Entidad}ValidatorTests
{
    private readonly {Crear}{Entidad}Validator _validator;

    public {Crear}{Entidad}ValidatorTests()
    {
        _validator = new {Crear}{Entidad}Validator();
    }

    [Fact]
    public void Validate_ComandoValido_DeberiaSerValido()
    {
        // Arrange
        var command = new {Crear}{Entidad}Command { {Propiedad} = "{valor}" };

        // Act
        var resultado = _validator.Validate(command);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_{Propiedad}Invalida_DeberiaFallar(string {propiedad})
    {
        // Arrange
        var command = new {Crear}{Entidad}Command { {Propiedad} = {propiedad} };

        // Act
        var resultado = _validator.Validate(command);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == nameof(command.{Propiedad}));
    }
}
```

## Tests de Entidades de Dominio
```csharp
public class {Entidad}Tests
{
    [Fact]
    public void Constructor_ParametrosValidos_DeberiaCrear{Entidad}()
    {
        // Arrange & Act
        var {entidad} = new {Entidad}("{param1}", "{param2}");

        // Assert
        {entidad}.{Propiedad}.ShouldBe("{param1}");
        {entidad}.FechaCreacion.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_{Propiedad}Invalida_DeberiaLanzarExcepcion(string {propiedad})
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new {Entidad}({propiedad}, "{param2}"));
    }
}
```

## Mejores Prácticas para Dapper

### Estrategias de Testing
- **Tests Unitarios (Handlers)**: Mockear `IAdoRepository`, testear lógica de negocio
- **Tests de Integración (Clases SQL)**: Base de datos real, validar stored procedures
- **Datos de prueba**: Usar Bogus para generar datos fake consistentes
- **Cleanup**: Implementar `IDisposable` para limpiar recursos

### Configuración Específica
- **TestContainers/LocalDB**: Para tests de integración con SQL Server
- **NSubstitute**: `Arg.Any<>()` para comandos/queries específicos, `Received()` para verificaciones
- **Shouldly**: Aserciones descriptivas (`valor.ShouldBe(esperado)`)

### Casos de Prueba Esenciales
- **Casos de éxito**: Camino feliz con datos válidos
- **Validaciones de negocio**: Reglas correctas antes de llamar stored procedures
- **Mapeo de datos**: Conversión correcta de resultados SQL a DTOs
- **Errores**: SqlException y errores de infraestructura
- **Casos límite**: Paginación, filtros, parámetros null/vacíos
- **Parámetros**: Verificar que se pasen los parámetros correctos con `Arg.Is<>()`

### Organización de Tests
- **Una clase de test por Handler/Validator**
- **Métodos privados**: Para setup común y generación de datos
- **Constructor**: Para inicialización de dependencias
- **Teorías**: Para múltiples casos similares con `[Theory]` e `[InlineData]`