# API REST Template

## Arquitectura

Este proyecto implementa una **API REST** siguiendo los principios de **Clean Architecture** con **Vertical Slices** y soporte para **Monolito Modular**.

### Capas de la Arquitectura

- **Domain**: Entidades del dominio y lógica de negocio central
- **Application**: Casos de uso organizados por features (CQRS + MediatR)
- **Infrastructure**: Acceso a datos, servicios externos e implementaciones técnicas
- **Api**: Capa de presentación con endpoints REST

### Características Principales

- **CQRS** con MediatR para separación de comandos y consultas
- **Vertical Slices** para organización por funcionalidades de negocio
- **Patrón Result** para manejo de errores sin excepciones
- **Entity Framework Core** o **Dapper + Stored Procedures** para acceso a datos s
- **FluentValidation** para validaciones
- **Minimal APIs** de .NET

### Estructura de Features

Cada funcionalidad se organiza como un slice vertical completo:

```
Application/{Entidad}/{Feature}/
├── {Operacion}{Entidad}Command.cs
├── {Operacion}{Entidad}CommandHandler.cs
├── {Operacion}{Entidad}Dto.cs
└── {Operacion}Endpoint.cs
```

## Documentación Completa

Para información detallada sobre patrones, convenciones y guías de desarrollo, consultar la **documentación oficial de la empresa**:

- Gss.MinimalApis - [🔗 **Documentación Técnica - Repositorio Público**](https://arquitectura-docs.apps.pro001.gss.com.ar/desarrollo-corporativo/be/gss-minimal-apis/vision-general)
- Arquetipos Backend - [🔗 **Documentación Técnica - Portal Interno**](https://arquitectura-docs.apps.pro001.gss.com.ar/arquetipos/backend/vision-general)

---

## Git Flow

Flujo de trabajo para desarrollo y despliegue:

1. **Desarrollo**: Crear feature branch desde `main`
2. **Testing**: Mergear feature a `testing` para pruebas QA
3. **Pre-producción**: Mergear feature a `pre` para validación final
4. **Producción**: Mergear feature a `main` tras aprobación

```
main ──┬─── feature/nueva-funcionalidad
       │    │
       │    ├─── testing (QA)
       │    ├─── pre (UAT)
       │    └─── main (PROD)
```

---

## Inicio Rápido

```bash
# Restaurar dependencias
dotnet restore

# Compilar solución
dotnet build

# Ejecutar API
dotnet run --project src/Api
```

La API estará disponible en: `https://localhost:7057/swagger`

### Logs locales en desarrollo

- El logging usa Serilog y, en desarrollo, el archivo `src/Api/logSettings.Development.json` tiene activado `Serilog:EnableLocalFileLogging` para escribir en `logs/development-.log` (rolling diario).
- Si querés cambiar la ruta o el template, ajustá la sección `Serilog:LocalFileLogging` en el mismo archivo antes de ejecutar la API.
