FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/IdentityService.Domain/IdentityService.Domain.csproj", "src/IdentityService.Domain/"]
COPY ["src/IdentityService.Application/IdentityService.Application.csproj", "src/IdentityService.Application/"]
COPY ["src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/IdentityService.Infrastructure/"]
COPY ["src/IdentityService.Api/IdentityService.Api.csproj", "src/IdentityService.Api/"]
RUN dotnet restore "src/IdentityService.Api/IdentityService.Api.csproj"

COPY . .
WORKDIR /src/src/IdentityService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
