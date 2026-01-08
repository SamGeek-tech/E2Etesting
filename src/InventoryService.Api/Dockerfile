FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/InventoryService.Api/InventoryService.Api.csproj", "src/InventoryService.Api/"]
COPY ["src/InventoryService.Application/InventoryService.Application.csproj", "src/InventoryService.Application/"]
COPY ["src/InventoryService.Domain/InventoryService.Domain.csproj", "src/InventoryService.Domain/"]
COPY ["src/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj", "src/InventoryService.Infrastructure/"]
RUN dotnet restore "./src/InventoryService.Api/InventoryService.Api.csproj"
COPY . .
WORKDIR "/src/src/InventoryService.Api"
RUN dotnet build "./InventoryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InventoryService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.Api.dll"]
