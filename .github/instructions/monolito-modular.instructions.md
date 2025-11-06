# Instrucciones para Arquitectura de Monolito Modular con Vertical Slices

## ⚠️ IMPORTANTE: Solicitar confirmación antes de proceder

**ANTES de crear cualquier estructura o realizar cambios**, debes:

1. **Explicar al desarrollador** la arquitectura de Monolito Modular con Vertical Slices que vas a implementar
2. **Mostrar la estructura de módulos** que planeas crear basándote en los dominios de negocio identificados
3. **Solicitar confirmación explícita** del desarrollador con una pregunta como:
   ```
   "¿Deseas proceder con la creación de esta estructura de Monolito Modular?
   Los módulos propuestos son: [listar módulos].
   ¿Es correcto o necesitas modificar algún módulo antes de continuar?"
   ```
4. **Solo proceder** después de recibir confirmación explícita del desarrollador

## Descripción General

Esta arquitectura combina los beneficios de un monolito (simplicidad de despliegue) con la modularidad (desarrollo independiente) utilizando Vertical Slices para organizar el código por funcionalidades de negocio.

### Principios Clave

- **Monolito Modular**: Una unidad desplegable única con módulos distintos por dominio de negocio
- **Vertical Slices**: Organización del código por funcionalidades completas (no por capas técnicas)
- **Aislamiento de Módulos**: Comunicación únicamente a través de APIs públicas
- **Separación de Datos**: Cada módulo mantiene su propia gestión de datos
- **Filtros de Solución**: Para optimizar el desarrollo en soluciones grandes

## Estructura del Proyecto

### Estructura Raíz

```
📁 SolutionRoot/
├── ⚙ [ModuleName]/                    # Un módulo por dominio de negocio
│   ├── 📦 [ModuleName].Domain/
│   ├── 📦 [ModuleName].Infrastructure/
│   ├── 📦 [ModuleName].Application/
│   └── 📦 [ModuleName].PublicApi/
├── 📁 Shared/                         # Código compartido entre módulos
│   ├── 📦 Shared.Domain/
│   ├── 📦 Shared.Infrastructure/
│   └── 📦 Shared.Application/
└── 📦 Api/ # Aplicación principal
├── 🗄 Solution.sln                    # Solución completa
└── 🗄 [ModuleName].slnf              # Filtros de solución por módulo
```

### Directrices por Capa

#### 1. Capa Domain (Dominio)

**Ubicación**: `[ModuleName].Domain/`

**Propósito**: Contiene la lógica de negocio central, entidades, enumeraciones y interfaces del dominio.

**Estructura**:

```
📦 [ModuleName].Domain/
├── 📁 Entities/         # Entidades del dominio
├── 📁 Enums/            # Enumeraciones
├── 📁 Interfaces/       # Interfaces del dominio
└── 📁 Events/           # Eventos de dominio
```

**Características**:

- Independiente de otras capas y módulos
- No debe referenciar infraestructura o tecnologías específicas
- Contiene la lógica de negocio pura

#### 2. Capa Infrastructure (Infraestructura)

**Ubicación**: `[ModuleName].Infrastructure/`

**Propósito**: Implementa los detalles técnicos como acceso a datos, servicios externos, etc.

**Estructura**:

```
📦 [ModuleName].Infrastructure/
├── 📁 Persistence/       # Configuración de acceso a datos
│   ├── 📁 Configurations/
│   └── 📁 Migrations/
├── 📁 Repositories/     # Implementaciones de repositorios
├── 📁 Services/         # Servicios de infraestructura
└── DependencyInjection.cs
```

**Características**:

- Cada módulo maneja su propia persistencia de datos
- Implementa interfaces definidas en Domain
- Registra servicios en el contenedor DI

#### 3. Capa Application (Aplicación)

**Ubicación**: `[ModuleName].Application/`

**Propósito**: Implementa casos de uso de negocio organizados como Vertical Slices.

**Estructura**:

```
📦 [ModuleName].Application/
├── 📁 CreateEntity/      # Vertical Slice completo según instrucciones generales del proyecto.
├── 📁 UpdateEntity/
├── 📁 GetEntity/
├── 📁 DeleteEntity/
└── DependencyInjection.cs
```

**Características**:

- Cada funcionalidad es una Vertical Slice completa
- Usa MediatR para el patrón CQRS
- Incluye validación con FluentValidation
- Implementa endpoints para la API

#### 4. Capa PublicApi (API Pública)

**Ubicación**: `[ModuleName].PublicApi/`

**Propósito**: Define interfaces para comunicación entre módulos.

**Estructura**:

```
📦 [ModuleName].PublicApi/
├── IModuleNameApi.cs     # Interface principal del módulo
├── 📁 Contracts/        # DTOs para comunicación entre módulos
└── DependencyInjection.cs
```

### Comunicación entre Módulos

#### Reglas de Comunicación

1. **Solo a través de APIs públicas**: Los módulos se comunican únicamente mediante las interfaces definidas en `PublicApi`
2. **Sin acceso directo a base de datos**: Ningún módulo puede acceder directamente a la base de datos de otro módulo
3. **Llamadas síncronas**: Dentro del mismo proceso usando inyección de dependencias
4. **Eventos opcionales**: Para comunicación asíncrona cuando se planifique migración a microservicios

### Cambios en solucion (.sln)

Agrega los proyectos y carpetas a la solucion mediante comandos CLI

```bash
dotnet sln add -s "src/[ModuleName]" [ModuleName]/[ModuleName].Domain/[ModuleName].Domain.csproj
dotnet sln add -s "src/[ModuleName]" [ModuleName]/[ModuleName].Infrastructure/[ModuleName].Infrastructure.csproj
dotnet sln add -s "src/[ModuleName]" [ModuleName]/[ModuleName].Application/[ModuleName].Application.csproj
dotnet sln add -s "src/[ModuleName]" [ModuleName]/[ModuleName].PublicApi/[ModuleName].PublicApi.csproj
```

### Filtros de Solución (.slnf)

#### Propósito

Los filtros de solución permiten cargar solo los proyectos relevantes para una tarea específica, mejorando el rendimiento del IDE en soluciones grandes.

#### Estructura de Filtro por Módulo

**Archivo**: `[ModuleName].slnf`

```json
{
  "solution": {
    "path": "Cross.ServiciosCross.BeneficiaApi.sln",
    "projects": [
      "[ModuleName]/[ModuleName].Domain/[ModuleName].Domain.csproj",
      "[ModuleName]/[ModuleName].Infrastructure/[ModuleName].Infrastructure.csproj",
      "[ModuleName]/[ModuleName].Application/[ModuleName].Application.csproj",
      "[ModuleName]/[ModuleName].PublicApi/[ModuleName].PublicApi.csproj",
      "Shared/Shared.Domain/Shared.Domain.csproj",
      "Shared/Shared.Infrastructure/Shared.Infrastructure.csproj",
      "Shared/Shared.Application/Shared.Application.csproj",
      "src/Api/Api.csproj"
    ]
  }
}
```

Luego de crear estos archivos, verifica que el contenido sea exactamente con este formato para evitar errores al abrir el filtro en Visual Studio.

### Configuración de la Aplicación Host

Los módulos se registran en la aplicación principal mediante métodos de extensión en `DependencyInjection.cs` de cada módulo, permitiendo configurar todos los servicios necesarios para el funcionamiento del módulo.

### Directrices de Implementación

#### 1. Creación de Nuevos Módulos

1. **Identificar el dominio de negocio**
2. **Crear la estructura de carpetas** siguiendo la convención
3. **Implementar las capas** en orden: Domain → Infrastructure → Application → PublicApi
4. **Registrar servicios** en el DI container
5. **Crear filtro de solución** para el módulo
6. **Agregar tests** correspondientes

#### 2. Naming Conventions

- **Módulos**: PascalCase en español sin acentos ni caracteres especiales (ej. `Envios`, `Inventario`, `Pedidos`)
- **Proyectos**: `[ModuleName].[LayerName]` (ej. `Envios.Domain`)
- **Namespaces**: `[ModuleName].[LayerName]`
- **Endpoints**: `/api/[modulename]/[resource]` (lowercase)

#### 3. Referencias entre Proyectos

```
Host.Api
├── Referencias a todos los [Module].Application
├── Referencias a todos los [Module].Infrastructure
└── Referencia a Shared.Infrastructure

[Module].Application
├── Referencia a [Module].Domain
├── Referencia a [Module].PublicApi
├── Referencias a otros [Module].PublicApi (si necesario)
└── Referencia a Shared.Application

[Module].Infrastructure
├── Referencia a [Module].Domain
├── Referencia a [Module].Application
└── Referencia a Shared.Infrastructure

[Module].PublicApi
└── Referencia a Shared.Domain (solo para tipos base)

[Module].Domain
└── Referencia a Shared.Domain (solo para tipos base)
```

#### 4. Base de Datos

- **Separación por módulo**: Cada módulo maneja su propia persistencia de datos
- **Migraciones independientes**: Cada módulo gestiona sus propias migraciones de base de datos
- **Aislamiento de datos**: Los módulos no deben acceder directamente a los datos de otros módulos

#### 5. Testing

```
test/
├── [ModuleName].Domain.Tests/
├── [ModuleName].Application.Tests/
├── [ModuleName].Infrastructure.Tests/
└── [ModuleName].Integration.Tests/
```

### [Application] Dependecy Injection

En los proyectos de Application de cada módulo, crea un archivo `DependencyInjection.cs` con los siguientes métodos de extensión:

```csharp
public static IServiceCollection AddModuleApplication(this IServiceCollection services)
{
    services.AddGssMediator(typeof(DependencyInjection).Assembly, options =>
    {
        options.EnableValidationPipeline = true;
        options.EnableLoggingPipeline = true;
    });
    return services;
}

public static WebApplication MapModuleEndpoints(this WebApplication app)
{
    app.MapEndpointsFromAssembly(typeof(DependencyInjection).Assembly);
        return app;
}
```

### [PublicApi] Dependecy Injection

En los proyectos de PublicApi de cada módulo, crea un archivo `DependencyInjection.cs` con el siguiente método de extensión:

```csharp
public static IServiceCollection AddModulePublicApi(this IServiceCollection services)
{
    services.AddScoped<IModuleApi, ModuleApi>();
    return services;
}
```

### [Infrastructure] Dependecy Injection

En los proyectos de Infrastructure de cada módulo, crea un archivo `DependencyInjection.cs` con el siguiente método de extensión:

```csharp
public static IServiceCollection AddModuleInfrastructure(this IServiceCollection services)
{
    // Si en la arquitectura actual ya existen registros de servicios/repositorios, migrarlos aquí
    return services;
}
```

### Centralización de paquetes

Recuerda que los paquetes NuGet deben ser gestionados desde Directory.Packages.props en la raíz de la solución para mantener versiones consistentes y que solo debes usar PackageReference en los archivos .csproj para referencias específicas de cada proyecto.

### Arquitectura Actual

Revisar la estructura de carpetas/archivos en los proyectos actuales para trasladarlas a la nueva estructura modular.

### Beneficios de esta Arquitectura

1. **Simplicidad**: Un único código base y despliegue
2. **Modularidad**: Límites claros entre dominios de negocio
3. **Escalabilidad**: Los equipos pueden trabajar independientemente en cada módulo
4. **Mantenibilidad**: Vertical Slices facilitan la localización y modificación de funcionalidades
5. **Flexibilidad**: Posible extracción futura a microservicios
6. **Rendimiento**: Filtros de solución optimizan la experiencia de desarrollo

### Consideraciones Importantes

1. **Evitar acoplamiento**: Los módulos deben comunicarse solo través de APIs públicas
2. **Gestión de transacciones**: Para operaciones que abarcan múltiples módulos, considerar patrones como Saga
3. **Versionado de APIs**: Las APIs entre módulos deben ser versionadas para evitar breaking changes
4. **Monitoreo**: Implementar logging y métricas a nivel de módulo para facilitar el debugging
