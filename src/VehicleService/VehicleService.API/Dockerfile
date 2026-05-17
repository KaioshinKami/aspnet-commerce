FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
COPY ["src/VehicleService/VehicleService.Domain/VehicleService.Domain.csproj", "src/VehicleService/VehicleService.Domain/"]
COPY ["src/VehicleService/VehicleService.Application/VehicleService.Application.csproj", "src/VehicleService/VehicleService.Application/"]
COPY ["src/VehicleService/VehicleService.Infrastructure/VehicleService.Infrastructure.csproj", "src/VehicleService/VehicleService.Infrastructure/"]
COPY ["src/VehicleService/VehicleService.API/VehicleService.API.csproj", "src/VehicleService/VehicleService.API/"]
RUN dotnet restore "src/VehicleService/VehicleService.API/VehicleService.API.csproj"
COPY . .
RUN dotnet build "src/VehicleService/VehicleService.API/VehicleService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/VehicleService/VehicleService.API/VehicleService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VehicleService.API.dll"]

