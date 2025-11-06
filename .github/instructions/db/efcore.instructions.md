---
applyTo: "src/**/Infrastructure/**/*.cs, src/**/Application/**/*.cs, src/**/Domain/**/*.cs"
---

# Entity Framework Core - Instrucciones de Desarrollo

## Estructura de Proyectos

### Organización de Archivos EF Core

```
src/
├── Domain/
│   ├── Entities/              # Entidades del dominio
│   └── Common/
│       └── IAuditableEntity.cs # Entidad base con auditoría
├── Application/
│   ├── Interfaces/
│   │   └── IAppDbContext.cs  # Interfaz del contexto
│   └── {Entidad}/
│       ├── {Entidad}EfExtensions.cs  # Extensiones IQueryable
│       └── {Features}/               # Features CQRS
└── Infrastructure/
    └── Persistence/
        ├── AppDbContext.cs           # Contexto principal
        ├── Configurations/           # Configuraciones EF
            ├── {Entidad}Configuration.cs
            └── ...
```

## 1. Configuración del Contexto de Base de Datos

### IAppDbContext (Application/Common/Interfaces/)

```csharp
/// <summary>
/// Interfaz que define el contrato para el contexto de base de datos de la aplicación.
/// </summary>
public interface IAppDbContext
{
    // DbSets para cada entidad
    DbSet<Usuario> Usuarios { get; set; }
    DbSet<Rol> Roles { get; set; }
    DbSet<Grupo> Grupos { get; set; }
    // ... otras entidades

    // Métodos esenciales
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    IDbContextTransaction BeginTransaction();
    Task<bool> CanConnectAsync(); // se utiliza en los healthchecks.
}
```

### AppDbContext (Infrastructure/Persistence/)

```csharp
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<Grupo> Grupos { get; set; } = null!;
    // ... otras entidades

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplicar todas las configuraciones del assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public IDbContextTransaction BeginTransaction()
        => Database.BeginTransaction();

    public async Task<bool> CanConnectAsync()
        => await Database.CanConnectAsync();
}
```

## 2. Configuraciones de Entidades (Infrastructure/Persistence/Configurations/)

### Ejemplo de Configuración

```csharp
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Configurar relaciones
        builder.HasMany(u => u.UsuarioRoles)
            .WithOne(ur => ur.Usuario)
            .HasForeignKey(ur => ur.UsuarioId);

        // Configuración de auditoría (si hereda de AuditableEntity)
        builder.Property(u => u.FechaCreacion)
            .IsRequired();

        builder.Property(u => u.CreadoPor)
            .HasMaxLength(50);

        builder.HasQueryFilter(u => !u.FechaEliminacion.HasValue); // Soft delete
    }
}
```

## 3. Entidades del Dominio

### Entidad Base Auditable (Domain/Common/)

```csharp
public interface IAuditableEntity
{
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public string? EliminadoPor { get; set; }
}
```

### Entidad de Dominio (Domain/Entities/)

```csharp
public class Usuario : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? ModificadoPor { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public string? EliminadoPor { get; set; }

    // Navegación
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
    public ICollection<UsuarioGrupo> UsuarioGrupos { get; set; } = new List<UsuarioGrupo>();
}
```

## 4. Extensiones de Consulta (Application/{Entidad}/)

### Extensiones IQueryable

```csharp
public static class UsuariosQueryExtensions
{
    public static IQueryable<Usuario> PorEmail(this IQueryable<Usuario> query, string email)
        => query.Where(u => u.Email == email);

    public static IQueryable<Usuario> Activos(this IQueryable<Usuario> query)
        => query.Where(u => u.Activo);

    public static IQueryable<Usuario> ConRoles(this IQueryable<Usuario> query)
        => query.Include(u => u.UsuarioRoles).ThenInclude(ur => ur.Rol);

    public static IQueryable<Usuario> BuscarPorTexto(this IQueryable<Usuario> query, string? texto)
        => query.WhereIfNotNull(texto, u => u.Nombre.Contains(texto!) || u.Email.Contains(texto!));

    public static IQueryable<Usuario> FiltrarPorActivo(this IQueryable<Usuario> query, bool? activo)
        => query.WhereIfNotNull(activo, u => u.Activo == activo);
}
```

## 5. Registro de Dependencias

### En Program.cs

```csharp
// Configurar Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // Solo en desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Registrar interfaz
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
```

## 6. Scaffolding Database First

### Configuración Inicial

Ejecutar desde el proyecto `Infrastructure`:

```bash
# Instalar herramientas EF (una sola vez por máquina)
dotnet tool install --global dotnet-ef

# Verificar instalación
dotnet ef
```

### Comando de Scaffolding Completo

Solicitar (si no existe) la cadena de conexión al desarrollador.

```pwsh
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir .\..\Domain\Entities \
  --context-dir Persistence \
  --context AppDbContext \
  -t <Tabla1> -t <Tabla2> -t <TablaN> \
  --data-annotations \
  --no-onconfiguring \
  --namespace Domain.Entities \
  --context-namespace Infrastructure.Persistence \
  --force
```

### Parámetros del Comando

- `<ConnectionString>`: String de conexión a la base de datos
- `--output-dir`: Directorio donde se generarán las entidades
- `--context-dir`: Directorio donde se generará el contexto
- `--context`: Nombre del contexto
- `-t <Tabla>`: Especificar tablas específicas (repetir por cada tabla)
- `--no-onconfiguring`: No generar OnConfiguring en el contexto
- `--namespace`: Namespace para las entidades
- `--context-namespace`: Namespace para el contexto
- `--force`: Sobrescribir archivos existentes

### Ejemplo Práctico

```bash
dotnet ef dbcontext scaffold "Server=localhost;Database=MiApp;Trusted_Connection=true;TrustServerCertificate=true;" Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir .\..\Domain\Entities \
  --context-dir Persistence \
  --context AppDbContext \
  -t Usuarios -t Roles -t Grupos -t UsuarioRoles -t UsuarioGrupos \
  --data-annotations \
  --no-onconfiguring \
  --namespace Domain.Entities \
  --context-namespace Infrastructure.Persistence \
  --force
```

### Post-Scaffolding

Después del scaffolding:

1. **Mover configuraciones a archivos separados**:

   ```csharp
   // En AppDbContext.cs, reemplazar OnModelCreating
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
       base.OnModelCreating(modelBuilder);
   }
   ```

2. **Crear configuraciones individuales** en `Infrastructure/Persistence/Configurations/`
3. **Implementar IAppDbContext** en el contexto generado
4. **Agregar implementación de interfaz de IAuditableEntity** donde corresponda
5. **Configurar query filters** para SoftDelete en las configuraciones

## 7. Patrones de Uso en Handlers

### Consultas de Solo Lectura

```csharp
public async Task<Result<UsuarioDto>> Handle(ObtenerUsuarioQuery request, CancellationToken cancellationToken)
{
    var usuario = await _context.Usuarios
        .AsNoTracking()  // Importante para consultas de solo lectura
        .ConRoles()      // Usar extensiones
        .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

    if (usuario == null)
        return Result<UsuarioDto>.NotFound("Usuario no encontrado");

    return Result<UsuarioDto>.Success(usuario.ToDto());
}
```

### Consultas Paginadas

```csharp
public async Task<PagedResult<UsuarioDto>> Handle(ObtenerUsuariosQuery request, CancellationToken cancellationToken)
{
    var query = _context.Usuarios
        .AsNoTracking()
        .Activos()
        .BuscarPorTexto(request.Busqueda)
        .FiltrarPorActivo(request.Activo)
        .OrderBy(u => u.Nombre);

    var total = await query.CountAsync(cancellationToken);
    var usuarios = await query
        .GetPage(request.Pagina, request.ItemsPorPagina)
        .ToListAsync(cancellationToken);

    return PagedResult<UsuarioDto>.Create(
        usuarios.Select(u => u.ToDto()),
        total
    );
}
```

### Comandos con Transacciones

```csharp
public async Task<Result> Handle(CrearUsuarioCommand request, CancellationToken cancellationToken)
{
    using var transaction = _context.BeginTransaction();

    try
    {
        var usuarioExistente = await _context.Usuarios
            .PorEmail(request.Email)
            .FirstOrDefaultAsync(cancellationToken);

        if (usuarioExistente != null)
            return Result.Conflict("Email ya registrado", "EMAIL_DUPLICADO");

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Email = request.Email,
            FechaCreacion = DateTime.UtcNow,
            CreadoPor = request.UsuarioCreador
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        transaction.Commit();
        return Result.Success();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

## 9. Mejores Prácticas

### Performance

- Usar `AsNoTracking()` para consultas de solo lectura
- Implementar paginación en listas grandes
- Usar `Include()` y `ThenInclude()` conscientemente

### Consultas Complejas

- Crear extensiones `IQueryable` reutilizables
- Aprovechar extensiones de `Gss.Linq` (`WhereIf`, `WhereIfNotNull`, etc.)

### Configuraciones

- Separar configuraciones EF en archivos individuales
- Usar `IEntityTypeConfiguration<T>`
- Aplicar configuraciones via `ApplyConfigurationsFromAssembly`
- Configurar query filters builder.HasQueryFilter(g => g.FechaBaja == null);

### Auditoría

- Implementar `IAuditableEntity` para entidades que requieren auditoría
- Usar interceptors para automatizar campos de auditoría

### Testing

- Usar `InMemoryDatabase` para pruebas unitarias
- Crear builders para entidades de prueba
- Implementar factories para contextos de prueba

## 10. Comandos de Utilidad

### Información del Modelo

```bash
# Ver información del modelo actual
dotnet ef dbcontext info --startup-project ..\Api

# Optimizar el modelo (generar código compilado)
dotnet ef dbcontext optimize --startup-project ..\Api
```

### Scaffolding Específico

```bash
# Scaffolding de tablas específicas con actualización
dotnet ef dbcontext scaffold "<ConnectionString>" Microsoft.EntityFrameworkCore.SqlServer \
  --output-dir .\..\Domain\Entities \
  --context-dir Persistence \
  --context AppDbContext \
  -t NuevaTabla \
  --data-annotations \
  --no-onconfiguring \
  --namespace Domain.Entities \
  --context-namespace Infrastructure.Persistence \
  --force
```

### Notas Importantes

- Siempre ejecutar comandos EF desde el proyecto que contiene el `DbContext` (Infrastructure)
- Especificar `--startup-project` para comandos que requieren configuración
- Usar `--force` con precaución en scaffolding para no sobrescribir cambios manuales
- Mantener scripts de migración bajo control de versiones
