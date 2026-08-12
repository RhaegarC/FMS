# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (cache-friendly)
COPY ["src/api/Fms.Api/Fms.Api.csproj", "src/api/Fms.Api/"]
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
