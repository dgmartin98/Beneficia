---
applyTo: "src/**/Application/**/*.cs"
---

# Stored Procedures con ADO.NET + Dapper - Instrucciones de Desarrollo

## Estructura General

El Repositorio `IAdoRepository` ya viene de caja incluido en Gss.CorporateApps.Data.Ado.Repositories. Este repositorio permite ejecutar comandos y consultas SQL, incluyendo stored procedures, utilizando ADO.NET y Dapper.

### Organización de Archivos
```
src/
├── Application/
│   └── {Entidad}/
│       ├── {Operacion}{Entidad}SqlCommand.cs      # CommandResult<T> / Command
│       ├── {Operacion}{Entidad}SqlQuery.cs        # Query<T>
│       └── {Operacion}{Entidad}SqlSingleQuery.cs  # SingleResult<T>
└── Infrastructure/
    └── Persistence/
        └── RepositoriesInstaller.cs  # Configuración DI
```

## 1. Tipos de Clases para Stored Procedures

### 1.1 CommandResult<T> - Comandos con Resultado
Para stored procedures que ejecutan cambios y retornan un resultado específico.

```csharp
using Gss.CorporateApps.Data.Ado.Entities;

namespace Application.{Entidad};

/// <summary>
/// Ejecuta el stored procedure para {descripción de la operación}.
/// Retorna un resultado con información del proceso.
/// </summary>
public class {Operacion}{Entidad}SqlCommand : CommandResult<{TipoResultado}>
{
    private readonly {TipoParametro1} _{nombreParametro1};
    private readonly {TipoParametro2} _{nombreParametro2};
    // ... otros parámetros

    public {Operacion}{Entidad}AdoCommand({TipoParametro1} {nombreParametro1}, 
        {TipoParametro2} {nombreParametro2})
    {
        _{nombreParametro1} = {nombreParametro1};
        _{nombreParametro2} = {nombreParametro2};
    }

    public override async Task<{TipoOutput}> Execute()
    {
        var query = DataAccess.CreateSpQuery("dbo.{NombreStoredProcedure}")
            .SetParameter("{parametro1}", _{nombreParametro1})
            .SetParameter("{parametro2}", _{nombreParametro2})
            .SetOutputParameter<{TipoOutput}>("{parametroOutput}");

        await query.ExecuteCommandAsync();
        
        var resultado = query.GetParameter<{TipoOutput}>("{parametroOutput}");

        return resultado;
    }
}
```

### 1.2 SingleResult<T> - Consulta de Registro Único
Para stored procedures que retornan un único resultado (consultas escalares o single row).

```csharp
using Gss.CorporateApps.Data.Ado.Entities;

namespace Application.{Entidad};

/// <summary>
/// Ejecuta el stored procedure para obtener {descripción}.
/// Retorna un único resultado.
/// </summary>
public class {Operacion}{Entidad}SqlSingleQuery : SingleResult<{TipoResultado}>
{
    private readonly {TipoParametro1} _{nombreParametro1};
    private readonly {TipoParametro2} _{nombreParametro2};

    public {Operacion}{Entidad}SqlSingleQuery({TipoParametro1} {nombreParametro1}, 
        {TipoParametro2} {nombreParametro2})
    {
        _{nombreParametro1} = {nombreParametro1};
        _{nombreParametro2} = {nombreParametro2};
    }

    public override async Task<{TipoResultado}> GetResult()
    {
        var query = DataAccess.CreateSpQuery("dbo.{NombreStoredProcedure}")
            .SetParameter("{parametro1}", _{nombreParametro1})
            .SetParameter("{parametro2}", _{nombreParametro2});

        var resultado = await query.Select(x => new TipoResultado
        {
            Propiedad1 = x.Columna1,
            Propiedad2 = x.Columna2
            // Mapear otras propiedades según sea necesario
        }).FirstOrDefaultAsync();

        return resultado;
    }
}
```

### 1.3 Query<T> - Consultas que Retornan Listas
Para stored procedures que retornan múltiples registros.

```csharp
using Gss.CorporateApps.Data.Ado.Entities;

namespace Application.{Entidad};

/// <summary>
/// Ejecuta el stored procedure para obtener una lista de {descripción}.
/// </summary>
public class {Operacion}{Entidad}SqlQuery : Query<{TipoDto}>
{
    private readonly {TipoParametro1} _{nombreParametro1};
    private readonly {TipoParametro2} _{nombreParametro2};

    public {Operacion}{Entidad}SqlQuery({TipoParametro1} {nombreParametro1}, 
        {TipoParametro2} {nombreParametro2})
    {
        _{nombreParametro1} = {nombreParametro1};
        _{nombreParametro2} = {nombreParametro2};
    }

    public override async Task<IEnumerable<{TipoDto}>> GetResult()
    {
        var query = DataAccess.CreateSpQuery("dbo.{NombreStoredProcedure}")
            .SetParameter("@{parametro1}", _{nombreParametro1})
            .SetParameter("@{parametro2}", _{nombreParametro2});

        var resultado = await query.Select(x => new TipoDto {
            Propiedad1 = x.Columna1,
            Propiedad2 = x.Columna2
            // Mapear otras propiedades según sea necesario
        }).ToListAsync();
        
        return resultado;
    }
}
```

### 1.4 Command - Comandos Sin Resultado
Para stored procedures que solo ejecutan cambios sin retornar información específica.

```csharp
using Gss.CorporateApps.Data.Ado.Entities;

namespace Application.{Entidad};

/// <summary>
/// Ejecuta el stored procedure para {descripción de la operación}.
/// No retorna resultado específico.
/// </summary>
public class {Operacion}{Entidad}SqlCommand : Command
{
    private readonly {TipoParametro1} _{nombreParametro1};
    private readonly {TipoParametro2} _{nombreParametro2};

    public {Operacion}{Entidad}SqlCommand({TipoParametro1} {nombreParametro1}, 
        {TipoParametro2} {nombreParametro2})
    {
        _{nombreParametro1} = {nombreParametro1};
        _{nombreParametro2} = {nombreParametro2};
    }

    public override async Task Execute()
    {
        var query = DataAccess.CreateSpQuery("dbo.{NombreStoredProcedure}")
            .SetParameter("@{parametro1}", _{nombreParametro1})
            .SetParameter("@{parametro2}", _{nombreParametro2});

        await query.ExecuteCommandAsync();
    }
}
```

## 2. Uso en Handlers

### 2.1 Handler con CommandResult
```csharp
public class {Operacion}{Entidad}Handler : ICommandHandler<{Operacion}{Entidad}Command>
{
    private readonly IAdoRepository _db;

    public {Operacion}{Entidad}Handler(IAdoRepository db)
    {
        _db = db;
    }

    public async Task<Result> Handle({Operacion}{Entidad}Command request, CancellationToken cancellationToken)
    {
        var comando = new {Operacion}{Entidad}SqlCommand(
            request.Parametro1,
            request.Parametro2
        );

        var resultado = await _db.ExecuteCommandAsync(comando);

        return Result.Success();
    }
}
```

### 2.2 Handler con SingleResult
```csharp
public class {Operacion}{Entidad}Handler : IQueryHandler<{Operacion}{Entidad}Query, {Entidad}Dto>
{
    private readonly IAdoRepository _db;

    public {Operacion}{Entidad}Handler(IAdoRepository db)
    {
        _db = db;
    }

    public async Task<Result<{Entidad}Dto>> Handle({Operacion}{Entidad}Query request, CancellationToken cancellationToken)
    {
        var consulta = new {Operacion}{Entidad}SqlSingleQuery(
            request.Parametro1,
            request.Parametro2
        );

        var resultado = await _db.GetFirstAsync(consulta);

        if (resultado == null)
            return Result<{Entidad}Dto>.NotFound("{Entidad} no encontrado");

        return Result<{Entidad}Dto>.Success(resultado);
    }
}
```

### 2.3 Handler con Query (Lista)
```csharp
public class {Operacion}{Entidad}Handler : IQueryHandler<{Operacion}{Entidad}Query, IEnumerable<{Entidad}Dto>>
{
    private readonly IAdoRepository _db;

    public {Operacion}{Entidad}Handler(IAdoRepository db)
    {
        _db = db;
    }

    public async Task<Result<IEnumerable<{Entidad}Dto>>> Handle({Operacion}{Entidad}Query request, CancellationToken cancellationToken)
    {
        var consulta = new {Operacion}{Entidad}SqlQuery(
            request.Parametro1,
            request.Parametro2
        );

        var resultados = await _db.QueryAsync(consulta);

        return Result<IEnumerable<{Entidad}Dto>>.Success(resultados);
    }
}
```

## 3. Patrones de Parámetros

### 3.1 Parámetros de Entrada
```csharp
// Parámetros básicos
.SetParameter("parametro", valor)

// Parámetros opcionales (nullable)
.SetParameter("parametroOpcional", valor ?? (object)DBNull.Value)

// Parámetros de fecha formateados
.SetParameter("@fecha", fecha.ToString("yyyy-MM-dd"))

// Parámetros de texto (con trim)
.SetParameter("@texto", texto?.Trim())
```

### 3.2 Parámetros de Salida
```csharp
// Parámetros de salida básicos
.SetOutputParameter<int>("outputId")
.SetOutputParameter<string>("outputMensaje")
.SetOutputParameter<int?>("outputOpcional")

// Obtener valores de parámetros de salida
var id = query.GetParameter<int>("outputId");
var mensaje = query.GetParameter<string>("outputMensaje");
var valorOpcional = query.GetParameter<int?>("outputOpcional");
```


### 4.2 Casos Comunes de Error
```csharp
// Error por registro no encontrado
if (id == null)
    return Result.NotFound("Registro no encontrado");

// Error por conflicto de datos
if (yaExiste)
    return Result.Conflict(
        "El registro ya existe", 
        "REGISTRO_DUPLICADO"
    );
```

## 5. Nomenclatura y Convenciones

### 5.1 Nombres de Clases
- **CommandResult**: `{Operacion}{Entidad}SqlCommand`
- **SingleResult**: `{Operacion}{Entidad}SqlSingleQuery`
- **Query**: `{Operacion}{Entidad}SqlQuery`
- **Command**: `{Operacion}{Entidad}SqlCommand`

### 5.2 Nombres de Parámetros
- Parámetros privados: `_{nombreParametro}` (camelCase con underscore)
- Parámetros de constructor: `{nombreParametro}` (camelCase)
- Parámetros SQL: `@{nombre_parametro}` (snake_case)

### 5.3 Documentación
```csharp
/// <summary>
/// Ejecuta el stored procedure '{NombreStoredProcedure}' para {descripción}.
/// {Información adicional sobre el comportamiento}.
/// </summary>
/// <param name="parametro1">Descripción del parámetro 1.</param>
/// <param name="parametro2">Descripción del parámetro 2.</param>
```

## 6. Configuración de Dependencias

### 6.1 Registro en DI Container
```csharp
// En Infrastructure/Persistence/RepositoriesInstaller.cs
public static IServiceCollection ConfigureRepositories(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.AddScoped<IAdoRepository>(_ => 
        new AdoRepository(configuration.GetConnectionString("DefaultConnection")));

    return services;
}
```

### 6.2 Uso en Program.cs
```csharp
// Registrar repositorios
builder.Services.ConfigureRepositories(builder.Configuration);
```

## 7. Mejores Prácticas

### 7.1 Performance
- Usar parámetros tipados para evitar conversiones innecesarias
- Establecer timeouts apropiados para SP de larga duración
- Paralelizar consultas independientes cuando sea posible

### 7.2 Seguridad
- Siempre usar parámetros, nunca concatenar strings
- Validar parámetros de entrada en el constructor
- Usar tipos nullable para parámetros opcionales
