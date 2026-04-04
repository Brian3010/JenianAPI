# Dockerfile — .NET 9 multi-stage build
# Place this file in the solution root or adjust COPY paths accordingly.

# -------- STAGE 1: Build / Publish --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files for all layers to enable restore caching
COPY src/Jenian.Domain/Jenian.Domain.csproj src/Jenian.Domain/
COPY src/Jenian.Application/Jenian.Application.csproj src/Jenian.Application/
COPY src/Jenian.Infrastructure/Jenian.Infrastructure.csproj src/Jenian.Infrastructure/
COPY src/Jenian.API/Jenian.API.csproj src/Jenian.API/

RUN dotnet restore src/Jenian.API/Jenian.API.csproj

# Copy all source code
COPY . .

RUN dotnet publish src/Jenian.API/Jenian.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# -------- STAGE 2: Runtime --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Jenian.API.dll"]
