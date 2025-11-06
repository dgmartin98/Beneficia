--- 
applyTo: test/**/*.cs
--- 

# Instrucciones para Tests Unitarios

## Principios Generales
- **Prioridad**: Handlers (lógica de negocio) > Validators > Endpoints
- Solo crear tests para funcionalidades principales y lógica de negocio crítica
- Enfocarse en los casos de éxito y fallo más importantes
- **Patrón AAA**: Arrange-Act-Assert (comentarios opcionales para claridad)

## Estructura y Organización
Si se utiliza una arquitectura monolitica modular, los tests deben reflejar la misma estructura, en cada uno de los módulos. 
### Ubicación
- **Application Tests**: `test/Application.Tests/{Entidad}/`
- **Infrastructure Tests**: `test/Infrastructure.Tests/`
- **Domain Tests**: `test/Domain.Tests/Entities/`

### Nomenclatura
- **Clase**: `{Operacion}{Entidad}Tests` (ej: `CrearUsuarioTests`)
- **Método**: `{Accion}_{Escenario}_{Resultado}` (ej: `Handle_UsuarioExistente_DeberiaRetornarConflict`)
- **Usar nombres descriptivos en español** que expliquen claramente el escenario

### Dependencias y Configuración
```xml
<!-- Package.Build.props -->
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="Shouldly" />
<PackageReference Include="NSubstitute" />
<PackageReference Include="NSubstitute.Analyzers.CSharp" />
<PackageReference Include="Bogus" /> <!-- Para datos fake -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" /> <!-- Para DbContext InMemory -->
```

```csharp
// GlobalUsings.cs
global using Gss.Results;
global using Shouldly;
global using Xunit;
global using NSubstitute;
global using Bogus;
global using Microsoft.EntityFrameworkCore;
```

## Patrones de Testing

### Tests de Handlers (OBLIGATORIO)

#### Command Handlers
```csharp
public class CrearUsuarioCommandHandlerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CrearUsuarioCommandHandler _handler;
    private readonly Faker _faker;

    public CrearUsuarioCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _handler = new CrearUsuarioCommandHandler(_context);
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

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        
        var usuarioCreado = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == command.Email);
        usuarioCreado.ShouldNotBeNull();
        usuarioCreado.Nombre.ShouldBe(command.Nombre);
    }

    [Fact]
    public async Task Handle_EmailDuplicado_DeberiaRetornarConflict()
    {
        // Arrange
        var emailExistente = "test@test.com";
        var usuarioExistente = new Usuario 
        { 
            Id = Guid.NewGuid(),
            Nombre = "Usuario Existente",
            Email = emailExistente 
        };
        
        _context.Usuarios.Add(usuarioExistente);
        await _context.SaveChangesAsync();

        var command = new CrearUsuarioCommand
        {
            Nombre = _faker.Person.FirstName,
            Email = emailExistente
        };

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeFalse();
        resultado.ErrorCode.ShouldBe("EMAIL_DUPLICADO");
        resultado.ErrorMessage.ShouldBe("El email ya está registrado");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

#### Query Handlers
```csharp
public class ObtenerUsuarioQueryHandlerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ObtenerUsuarioQueryHandler _handler;

    public ObtenerUsuarioQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _handler = new ObtenerUsuarioQueryHandler(_context);
    }

    [Fact]
    public async Task Handle_UsuarioExiste_DeberiaRetornarUsuario()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var usuario = new Usuario 
        { 
            Id = usuarioId, 
            Nombre = "Juan Pérez",
            Email = "juan@test.com"
        };
        
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var query = new ObtenerUsuarioQuery { Id = usuarioId };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Value.ShouldNotBeNull();
        resultado.Value.Id.ShouldBe(usuarioId);
        resultado.Value.Nombre.ShouldBe("Juan Pérez");
    }

    [Fact]
    public async Task Handle_UsuarioNoExiste_DeberiaRetornarNotFound()
    {
        // Arrange
        var query = new ObtenerUsuarioQuery { Id = Guid.NewGuid() };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeFalse();
        resultado.ErrorMessage.ShouldBe("Usuario no encontrado");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

#### Paged Query Handlers
```csharp
public class ObtenerUsuariosQueryHandlerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ObtenerUsuariosQueryHandler _handler;
    private readonly Faker _faker;

    public ObtenerUsuariosQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new AppDbContext(options);
        _handler = new ObtenerUsuariosQueryHandler(_context);
        _faker = new Faker("es");
    }

    [Fact]
    public async Task Handle_ConsultaValida_DeberiaRetornarResultadoPaginado()
    {
        // Arrange
        var usuarios = GenerarUsuariosFake(10);
        _context.Usuarios.AddRange(usuarios);
        await _context.SaveChangesAsync();

        var query = new ObtenerUsuariosQuery { Pagina = 1, ItemsPorPagina = 5 };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.ShouldBeTrue();
        resultado.Items.Count().ShouldBe(5);
        resultado.TotalCount.ShouldBe(10);
        resultado.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_FiltroNombre_DeberiaFiltrarCorrectamente()
    {
        // Arrange
        var usuarios = new List<Usuario>
        {
            new Usuario { Id = Guid.NewGuid(), Nombre = "Juan Pérez", Email = "juan@test.com" },
            new Usuario { Id = Guid.NewGuid(), Nombre = "María García", Email = "maria@test.com" },
            new Usuario { Id = Guid.NewGuid(), Nombre = "Pedro López", Email = "pedro@test.com" }
        };
        
        _context.Usuarios.AddRange(usuarios);
        await _context.SaveChangesAsync();

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
    }

    private List<Usuario> GenerarUsuariosFake(int cantidad)
    {
        var faker = new Faker<Usuario>("es")
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Nombre, f => f.Person.FirstName)
            .RuleFor(u => u.Email, f => f.Internet.Email());
        
        return faker.Generate(cantidad);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

### Tests de Validators
```csharp
public class CrearUsuarioValidatorTests
{
    private readonly CrearUsuarioValidator _validator;

    public CrearUsuarioValidatorTests()
    {
        _validator = new CrearUsuarioValidator();
    }

    [Fact]
    public void Validate_ComandoValido_DeberiaSerValido()
    {
        // Arrange
        var command = new CrearUsuarioCommand
        {
            Nombre = "Juan Pérez",
            Email = "juan@test.com"
        };

        // Act
        var resultado = _validator.Validate(command);

        // Assert
        resultado.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_NombreInvalido_DeberiaFallar(string nombre)
    {
        // Arrange
        var command = new CrearUsuarioCommand
        {
            Nombre = nombre,
            Email = "test@test.com"
        };

        // Act
        var resultado = _validator.Validate(command);

        // Assert
        resultado.IsValid.ShouldBeFalse();
        resultado.Errors.ShouldContain(e => e.PropertyName == nameof(command.Nombre));
    }
}
```

### Tests de Entidades de Dominio
```csharp
public class UsuarioTests
{
    [Fact]
    public void Constructor_ParametrosValidos_DeberiaCrearUsuario()
    {
        // Arrange & Act
        var usuario = new Usuario("Juan", "juan@test.com");

        // Assert
        usuario.Nombre.ShouldBe("Juan");
        usuario.Email.ShouldBe("juan@test.com");
        usuario.FechaCreacion.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_NombreInvalido_DeberiaLanzarExcepcion(string nombre)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new Usuario(nombre, "test@test.com"));
    }
}
```

## Mejores Prácticas

### DbContext InMemory para Tests de Handlers
- **Base de datos única**: Usar `Guid.NewGuid().ToString()` como nombre para evitar conflictos
- **Dispose Pattern**: Implementar `IDisposable` en clases de test para limpiar el contexto
- **Datos de prueba**: Usar `AddRange()` y `SaveChangesAsync()` para preparar datos
- **Verificaciones**: Consultar directamente la base de datos para verificar cambios

### Datos de Prueba
- **Bogus**: Para generar datos fake consistentes y realistas
- **Configurar Locale**: `new Faker("es")` para datos en español
- **Builders**: Crear métodos helper para objetos complejos

### Mocking con NSubstitute
- **Servicios externos**: Mockear solo servicios externos e interfaces que no sean EF Core
- **Configuración específica**: Usar `Arg.Any<>()` con tipos específicos
- **Verificaciones**: Verificar llamadas importantes con `Received()`

### Aserciones con Shouldly
- **Descriptivas**: `valor.ShouldBe(esperado)` en lugar de `Assert.Equal`
- **Colecciones**: `lista.ShouldContain()`, `lista.Count().ShouldBe()`
- **Excepciones**: `Should.Throw<TException>()`

### Casos de Prueba Esenciales
- **Casos de éxito**: Camino feliz con datos válidos
- **Validaciones**: Datos inválidos, nulos, vacíos
- **Lógica de negocio**: Reglas específicas del dominio
- **Errores comunes**: NotFound, Conflict, Forbidden
- **Casos límite**: Paginación, filtros, ordenamiento

### Organización de Tests
- **Una clase de test por Handler/Validator**
- **Métodos privados**: Para setup común y generación de datos
- **Constructor**: Para inicialización de dependencias
- **Teorías**: Para múltiples casos similares con `[Theory]` e `[InlineData]`