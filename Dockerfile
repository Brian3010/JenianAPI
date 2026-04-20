# ============================================================
# Dockerfile for Jenian API  –  .NET 9 / ASP.NET Core
# Multi-stage build: compile in the SDK image, run in the
# smaller ASP.NET Core runtime image (Linux / Debian 12).
#
# Build context MUST be the repository root so that all four
# projects under src/ are visible:
#   docker build -t jenian-api .
# ============================================================

# ------------------------------------------------------------
# Stage 1 – BUILD
# Uses the full .NET SDK image (larger) so it can compile code
# and restore NuGet packages.  This stage is NOT shipped to
# production; only its output is copied to Stage 2.
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /repo

# --- Copy project / solution files first (NuGet restore layer) ---
# Copying only the .csproj and .sln files before the rest of the
# source lets Docker cache the NuGet restore step.  If your code
# changes but the .csproj files don't, this layer is reused and
# the restore is skipped – making rebuilds much faster.
COPY JenianAPI.sln   ./
COPY global.json     ./
COPY src/Jenian.Domain/Jenian.Domain.csproj                   src/Jenian.Domain/
COPY src/Jenian.Application/Jenian.Application.csproj         src/Jenian.Application/
COPY src/Jenian.Infrastructure/Jenian.Infrastructure.csproj   src/Jenian.Infrastructure/
COPY src/Jenian.API/Jenian.API.csproj                         src/Jenian.API/

# Restore all NuGet packages declared across the solution.
# The -r flag targets linux-x64 so the correct native assets
# (e.g. OpenCvSharp4.runtime.linux) are resolved.
RUN dotnet restore src/Jenian.API/Jenian.API.csproj \
    -r linux-x64

# --- Copy the rest of the source code ---
# Done after restore so the cache above is not busted by a
# simple source-code change.
COPY src/ src/

# Publish the API project in Release configuration.
# -r linux-x64          → target Linux 64-bit
# --self-contained false → rely on the runtime image's .NET install
# -o /app/publish       → output directory
RUN dotnet publish src/Jenian.API/Jenian.API.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app/publish

# ------------------------------------------------------------
# Stage 2 – RUNTIME
# Uses only the ASP.NET Core runtime image (much smaller than
# the SDK image).  Contains NO compiler or source code.
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install native libraries that OpenCvSharp4 needs on Debian/Linux.
# libgomp1     – OpenMP (parallel processing used by OpenCV)
# libglib2.0-0 – GLib (dependency of several OpenCV modules)
# libgdiplus   – GDI+ compatibility layer (used by System.Drawing)
# Cleaning the apt cache keeps the image layer small.
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgomp1 \
    libglib2.0-0 \
    libfreetype6 \
    libharfbuzz0b \
    libgtk-3-0 \
    libpangocairo-1.0-0 \
    libpango-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libcairo2 \
    libgdk-pixbuf-2.0-0 \
    libdrm2 \
    libatomic1 \
    libx11-6 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the published binaries from the build stage.
# Only the compiled output travels to the runtime image –
# no source code, no SDK, no NuGet cache.
COPY --from=build /app/publish .

RUN find /app -name "*OpenCvSharp*" -print
RUN sh -c 'for f in $(find /app -name "libOpenCvSharpExtern.so"); do echo "== $f =="; ldd "$f"; done' || true

# Port 8080 is the default HTTP port for ASP.NET Core in Docker
# since .NET 8 (changed from 80).  Azure Container Apps and
# App Service also default to probing port 8080.
EXPOSE 8080

# Tell ASP.NET Core to listen on HTTP port 8080.
# Azure handles TLS termination at the load-balancer level, so
# the container itself only needs HTTP.
ENV ASPNETCORE_HTTP_PORTS=8080

# Run as Production by default.  Override with
# -e ASPNETCORE_ENVIRONMENT=Development for local testing.
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application.
ENTRYPOINT ["dotnet", "Jenian.API.dll"]
