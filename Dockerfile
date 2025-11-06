# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia archivos de proyecto para restaurar dependencias
COPY ["src/Api/Api.csproj", "src/Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["Directory.Packages.props", "."]
COPY ["nuget.config", "."]

# Restaura las dependencias
RUN dotnet restore "src/Api/Api.csproj"

# Copia el resto del código fuente
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build /clp:ErrorsOnly -m

# Etapa de publish
FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish -m --self-contained true -r linux-x64 /clp:ErrorsOnly /p:PublishReadyToRunComposite=true

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
# Setea la zona horaria a, por ejemplo, "America/Argentina/Buenos_Aires"
ENV TZ="America/Argentina/Buenos_Aires"


WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"] 