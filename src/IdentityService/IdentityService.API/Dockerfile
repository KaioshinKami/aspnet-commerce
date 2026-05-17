FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
COPY ["src/IdentityService/IdentityService.Domain/IdentityService.Domain.csproj", "src/IdentityService/IdentityService.Domain/"]
COPY ["src/IdentityService/IdentityService.Application/IdentityService.Application.csproj", "src/IdentityService/IdentityService.Application/"]
COPY ["src/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/IdentityService/IdentityService.Infrastructure/"]
COPY ["src/IdentityService/IdentityService.API/IdentityService.API.csproj", "src/IdentityService/IdentityService.API/"]
RUN dotnet restore "src/IdentityService/IdentityService.API/IdentityService.API.csproj"
COPY . .
RUN dotnet build "src/IdentityService/IdentityService.API/IdentityService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/IdentityService/IdentityService.API/IdentityService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.API.dll"]

