# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (cache-friendly). Every project's csproj is copied so
# the layered solution's project references (Api → Service/Repository/Interface/Model)
# resolve during restore before the rest of the source arrives.
COPY ["src/api/Fms.Api/Fms.Api.csproj", "src/api/Fms.Api/"]
COPY ["src/api/Fms.Interface/Fms.Interface.csproj", "src/api/Fms.Interface/"]
COPY ["src/api/Fms.Model/Fms.Model.csproj", "src/api/Fms.Model/"]
COPY ["src/api/Fms.Repository/Fms.Repository.csproj", "src/api/Fms.Repository/"]
COPY ["src/api/Fms.Service/Fms.Service.csproj", "src/api/Fms.Service/"]
RUN dotnet restore "src/api/Fms.Api/Fms.Api.csproj"

# Build + publish
COPY . .
WORKDIR "/src/src/api/Fms.Api"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fms.Api.dll"]
