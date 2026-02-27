# Dockerfile — .NET 8 multi‑stage build
# Place this file NEXT TO: program.cs and JenianAPI.csproj

# Mental model (JS analogy):
# - Stage 1 ("build") is like a CI job that runs:
#     npm install + npm run build
# - Stage 2 ("runtime") is like the tiny production container that only contains:
#     the compiled output + the runtime needed to execute it
#
# Why multi-stage?
# - dotnet/sdk image is BIG (has compilers, restore tooling)
# - dotnet/aspnet image is smaller (runtime only)
# - result: smaller image, faster deploys, less attack surface

# -------- STAGE 1: Build / Publish (like "npm run build") --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# FROM = pick the base image.
# dotnet/sdk includes:
# - dotnet restore/build/publish tools
# - compilers
# Use the SAME major version as your TargetFramework

WORKDIR /src
# WORKDIR sets the working directory inside the container.
# Equivalent to: cd /src

# 1) Copy ONLY the project file first
COPY JenianAPI.csproj ./
# COPY <source> <dest>
# This copies just the csproj to /src/JenianAPI.csproj
#
# Why only csproj first?
# Docker caches layers.
# If your code changes, but csproj doesn't, Docker can reuse the "restore" layer.

RUN dotnet restore ./JenianAPI.csproj
# RUN executes a command at build time (creates a new cached layer).
# dotnet restore downloads NuGet packages based on your csproj.
# Similar idea to: npm install / pnpm install


# 2) Copy the rest of your source code
COPY . ./
# Now copy everything else (Program.cs, Controllers, etc.)
# If you did this earlier, any code change would bust the cache and force restore again.

RUN dotnet publish ./JenianAPI.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false
# dotnet publish = builds + packages the app for deployment.
#
# -c Release        -> optimized build (prod)
# -o /app/publish   -> output folder inside the container
# --no-restore      -> we already restored earlier
# /p:UseAppHost=false -> avoids generating a platform-specific "app host" executable.
#                        Not required, but reduces output size and avoids weirdness in containers.
#
# Output includes:
# - JenianAPI.dll (+ dependencies)
# - config files (appsettings.json etc.)
# - any static content
#
# NOTE: publish output is what you deploy to production


# -------- STAGE 2: Runtime (small, production image) --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
# Runtime image only.
# No build tools. Smaller and safer.

WORKDIR /app
# cd /app

ENV ASPNETCORE_HTTP_PORTS=8080
# ENV sets environment variables INSIDE the container.
#
# Kestrel (ASP.NET server) needs to know which port to listen on.
# ASPNETCORE_HTTP_PORTS=8080 means:
# - Listen on port 8080 on all network interfaces (0.0.0.0).
#
# Equivalent old style is:
# ENV ASPNETCORE_URLS=http://0.0.0.0:8080
# Both are OK; HTTP_PORTS is a newer clean setting.

EXPOSE 8080
# EXPOSE is documentation for the container (and helpful for some tooling).
# It does NOT publish the port by itself.
# Publishing happens when you run:
#   docker run -p 8080:8080 ...
# or when a host like Azure maps ports to your container.

COPY --from=build /app/publish ./
# Copy publish output from the "build" stage into /app in the runtime stage.
#
# This is the magic of multi-stage:
# - final image contains only runtime + published output
# - no SDK, no source code, no NuGet caches

ENTRYPOINT ["dotnet", "JenianAPI.dll"]
# ENTRYPOINT defines what runs when the container starts.
# This is the "production start command".
#
# Equivalent to running locally:
#   dotnet JenianAPI.dll
#
# If your dll name differs, update it.
