FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared.Contracts/Shared.Contracts.csproj", "src/Shared.Contracts/"]
COPY ["src/BookingService/BookingService.Domain/BookingService.Domain.csproj", "src/BookingService/BookingService.Domain/"]
COPY ["src/BookingService/BookingService.Application/BookingService.Application.csproj", "src/BookingService/BookingService.Application/"]
COPY ["src/BookingService/BookingService.Infrastructure/BookingService.Infrastructure.csproj", "src/BookingService/BookingService.Infrastructure/"]
COPY ["src/BookingService/BookingService.API/BookingService.API.csproj", "src/BookingService/BookingService.API/"]
RUN dotnet restore "src/BookingService/BookingService.API/BookingService.API.csproj"
COPY . .
RUN dotnet build "src/BookingService/BookingService.API/BookingService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/BookingService/BookingService.API/BookingService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.API.dll"]

