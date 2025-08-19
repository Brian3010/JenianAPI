# Dockerfile — .NET 8 multi‑stage build
# Place this file NEXT TO: program.cs and JenianAPI.csproj

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1) copy csproj first (better layer caching)
COPY ./JenianAPI.csproj ./
RUN dotnet restore

# 2) copy the rest and publish
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore /p:UseAppHost=false

# ---- runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app ./

# Kestrel listens on 8080 inside the container 
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

# If your dll name differs, adjust it here
ENTRYPOINT ["dotnet", "JenianAPI.dll"]
